using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IPurchaseOrderService : ICrudService<PurchaseOrder>
{
    Task<List<PurchaseOrder>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default);
    Task<int> GetOpenCountAsync(CancellationToken cancellationToken = default);
}
