using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IMaterialRequestTemplateService : ICrudService<MaterialRequestTemplate>
{
    Task<MaterialRequestTemplate?> GetTemplateWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MaterialRequestTemplateLine?> GetLineByIdAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default);
    Task AddLineAsync(MaterialRequestTemplateLine line, CancellationToken cancellationToken = default);
    Task UpdateLineAsync(MaterialRequestTemplateLine line, CancellationToken cancellationToken = default);
    Task DeleteLineAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default);
    Task<int> ApplyTemplateAsync(Guid templateId, Guid? projectId, DateTime neededByDate, string? requestedByUserId, CancellationToken cancellationToken = default);
}
