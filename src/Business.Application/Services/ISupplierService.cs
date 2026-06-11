using Business.Domain.Entities;

namespace Business.Application.Services;

public interface ISupplierService : ICrudService<Supplier>
{
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<List<Supplier>> SearchAsync(string? query, CancellationToken cancellationToken = default);
}
