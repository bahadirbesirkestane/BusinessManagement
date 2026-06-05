using Business.Domain.Entities;

namespace Business.Application.Services;

public interface ICustomerService : ICrudService<Customer>
{
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
