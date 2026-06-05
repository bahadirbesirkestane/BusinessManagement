using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class SupplierService : CrudService<Supplier>, ISupplierService
{
    public SupplierService(IRepository<Supplier> repository) : base(repository)
    {
    }

    protected override IQueryable<Supplier> ListQuery()
    {
        return Repository.Query().OrderBy(x => x.Name);
    }

    protected override IQueryable<Supplier> DetailsQuery()
    {
        return Repository.Query()
            .Include(x => x.PurchaseOrders)
                .ThenInclude(x => x.Project)
            .Include(x => x.PurchaseOrders)
                .ThenInclude(x => x.Material);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Repository.Query().CountAsync(cancellationToken);
    }
}
