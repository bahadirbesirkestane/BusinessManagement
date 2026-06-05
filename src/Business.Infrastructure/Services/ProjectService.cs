using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class ProjectService : CrudService<Project>, IProjectService
{
    public ProjectService(IRepository<Project> repository) : base(repository)
    {
    }

    protected override IQueryable<Project> ListQuery()
    {
        return Repository.Query().Include(x => x.Customer);
    }

    protected override IQueryable<Project> DetailsQuery()
    {
        return Repository.Query()
            .Include(x => x.Customer)
            .Include(x => x.Tasks).ThenInclude(x => x.TaskCategory)
            .Include(x => x.Tasks).ThenInclude(x => x.Assignments)
            .Include(x => x.PurchaseOrders).ThenInclude(x => x.Supplier)
            .Include(x => x.MaterialRequests)
            .Include(x => x.Updates)
            .Include(x => x.CostItems)
            .Include(x => x.Invoices);
    }

    public Task<List<Project>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default)
    {
        return ListQuery().OrderByDescending(x => x.CreatedAt).Take(count).ToListAsync(cancellationToken);
    }

    public Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        return Repository.Query().CountAsync(x => x.Status != ProjectStatus.Completed && x.Status != ProjectStatus.Cancelled, cancellationToken);
    }
}
