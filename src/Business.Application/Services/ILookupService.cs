using Business.Domain.Entities;

namespace Business.Application.Services;

public interface ILookupService
{
    Task<List<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<List<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<List<Material>> GetMaterialsAsync(CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default);
}
