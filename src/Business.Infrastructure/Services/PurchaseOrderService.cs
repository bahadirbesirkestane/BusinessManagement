using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class PurchaseOrderService : CrudService<PurchaseOrder>, IPurchaseOrderService
{
    public PurchaseOrderService(IRepository<PurchaseOrder> repository) : base(repository)
    {
    }

    protected override IQueryable<PurchaseOrder> ListQuery()
    {
        return Repository.Query()
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material);
    }

    protected override IQueryable<PurchaseOrder> DetailsQuery()
    {
        return ListQuery().Include(x => x.Invoices);
    }

    public Task<List<PurchaseOrder>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default)
    {
        return ListQuery().OrderByDescending(x => x.CreatedAt).Take(count).ToListAsync(cancellationToken);
    }

    public Task<int> GetOpenCountAsync(CancellationToken cancellationToken = default)
    {
        return Repository.Query().CountAsync(x => x.Status != PurchaseOrderStatus.Delivered && x.Status != PurchaseOrderStatus.Cancelled, cancellationToken);
    }
}
