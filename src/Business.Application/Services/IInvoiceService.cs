using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IInvoiceService : ICrudService<Invoice>
{
    Task<List<Invoice>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default);
}
