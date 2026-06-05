using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class LookupService : ILookupService
{
    private readonly ApplicationDbContext _context;

    public LookupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        return _context.Customers.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Projects.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<List<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        return _context.Suppliers.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<List<Material>> GetMaterialsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Materials.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _context.PurchaseOrders.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
}
