using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

    public Task<List<Supplier>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var suppliers = ListQuery();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            suppliers = suppliers.Where(x =>
                x.Name.Contains(term) ||
                (x.Type != null && x.Type.Contains(term)) ||
                (x.Email != null && x.Email.Contains(term)) ||
                (x.Phone != null && x.Phone.Contains(term)) ||
                (x.PaymentTerm != null && x.PaymentTerm.Contains(term)) ||
                (x.Address != null && x.Address.Contains(term)) ||
                (x.Website != null && x.Website.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)));
        }

        return suppliers.ToListAsync(cancellationToken);
    }
}
