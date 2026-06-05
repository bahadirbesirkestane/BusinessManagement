using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IProjectTimelineService
{
    Task AddAsync(Guid projectId, string title, string? description = null, CancellationToken cancellationToken = default);
    Task AddForTaskAsync(Guid taskId, string title, string? description = null, CancellationToken cancellationToken = default);
    Task AddForOrderAsync(Guid orderId, string title, string? description = null, CancellationToken cancellationToken = default);
}
