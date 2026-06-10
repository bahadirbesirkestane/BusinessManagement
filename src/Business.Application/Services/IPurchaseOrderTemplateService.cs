using Business.Domain.Entities;

namespace Business.Application.Services;

public interface IPurchaseOrderTemplateService : ICrudService<PurchaseOrderTemplate>
{
    Task<PurchaseOrderTemplate?> GetTemplateWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderTemplateLine?> GetLineByIdAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default);
    Task AddLineAsync(PurchaseOrderTemplateLine line, CancellationToken cancellationToken = default);
    Task UpdateLineAsync(PurchaseOrderTemplateLine line, CancellationToken cancellationToken = default);
    Task DeleteLineAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default);
}
