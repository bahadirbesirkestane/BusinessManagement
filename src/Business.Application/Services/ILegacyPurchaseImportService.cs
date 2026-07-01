namespace Business.Application.Services;

public interface ILegacyPurchaseImportService
{
    Task<LegacyPurchaseImportResult> ImportAsync(Stream workbookStream, CancellationToken cancellationToken = default);
}
