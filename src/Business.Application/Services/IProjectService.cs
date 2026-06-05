using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IProjectService : ICrudService<Project>
{
    Task<List<Project>> GetRecentAsync(int count = 6, CancellationToken cancellationToken = default);
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
}
