using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IPersonalTaskService
{
    Task<List<PersonalTask>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PersonalTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PersonalTask?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(PersonalTask entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(PersonalTask entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
