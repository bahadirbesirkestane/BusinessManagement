using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class InvoiceService : CrudService<Invoice>, IInvoiceService
{
    public InvoiceService(IRepository<Invoice> repository) : base(repository)
    {
    }

    protected override IQueryable<Invoice> ListQuery()
    {
        return Repository.Query()
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .Include(x => x.Project)
            .Include(x => x.PurchaseOrder);
    }

    protected override IQueryable<Invoice> DetailsQuery()
    {
        return ListQuery().Include(x => x.Lines).ThenInclude(x => x.Material);
    }

    public Task<List<Invoice>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default)
    {
        return ListQuery().OrderByDescending(x => x.IssueDate).Take(count).ToListAsync(cancellationToken);
    }
}
