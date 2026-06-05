using Business.Domain.Entities;

namespace Business.Application.Services;

public interface ISupplierService : ICrudService<Supplier>
{
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
