using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IMaterialRequestService : ICrudService<MaterialRequest>
{
    Task<List<MaterialRequest>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}
