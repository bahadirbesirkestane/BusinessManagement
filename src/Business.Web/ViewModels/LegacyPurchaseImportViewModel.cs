using Business.Application.Services;

namespace Business.Web.ViewModels;

public class LegacyPurchaseImportViewModel
{
    public LegacyPurchaseImportResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TraceId { get; set; }
}
