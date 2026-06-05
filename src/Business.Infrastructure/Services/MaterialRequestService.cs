using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class MaterialRequestService : CrudService<MaterialRequest>, IMaterialRequestService
{
    public MaterialRequestService(IRepository<MaterialRequest> repository) : base(repository)
    {
    }

    protected override IQueryable<MaterialRequest> ListQuery()
    {
        return Repository.Query()
            .Include(x => x.Project)
            .Include(x => x.Material);
    }

    public Task<List<MaterialRequest>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default)
    {
        return ListQuery().OrderByDescending(x => x.CreatedAt).Take(count).ToListAsync(cancellationToken);
    }

    public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return Repository.Query().CountAsync(x => x.Status == MaterialRequestStatus.Requested, cancellationToken);
    }
}
