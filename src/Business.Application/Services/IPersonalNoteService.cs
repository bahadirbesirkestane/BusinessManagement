using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IPersonalNoteService
{
    Task<List<PersonalNote>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PersonalNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PersonalNote?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(PersonalNote entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(PersonalNote entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
