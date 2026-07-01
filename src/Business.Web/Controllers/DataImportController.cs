using Business.Application.Services;
using Business.Infrastructure.Identity;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Business.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class DataImportController : Controller
{
    private readonly ILegacyPurchaseImportService _legacyPurchaseImportService;
    private readonly ILogger<DataImportController> _logger;

    public DataImportController(ILegacyPurchaseImportService legacyPurchaseImportService, ILogger<DataImportController> logger)
    {
        _legacyPurchaseImportService = legacyPurchaseImportService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new LegacyPurchaseImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LegacyPurchases(IFormFile? file, CancellationToken cancellationToken)
    {
        var model = new LegacyPurchaseImportViewModel();

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Lütfen içe aktarılacak Excel dosyasını seçin.");
            return View(nameof(Index), model);
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(file), "Yalnızca .xlsx dosyaları destekleniyor.");
            return View(nameof(Index), model);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            model.Result = await _legacyPurchaseImportService.ImportAsync(stream, cancellationToken);
            ViewBag.Success = "Excel dosyası başarıyla içeri aktarıldı.";
        }
        catch (Exception exception)
        {
            model.TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            model.ErrorMessage = "Excel içe aktarımı sırasında hata oluştu. Ayrıntılar loglara yazıldı.";

            _logger.LogError(
                exception,
                "Legacy purchase import failed. TraceId: {TraceId}, FileName: {FileName}, Length: {Length}",
                model.TraceId,
                file.FileName,
                file.Length);

            ModelState.AddModelError(string.Empty, $"{model.ErrorMessage} Takip kodu: {model.TraceId}");
        }

        return View(nameof(Index), model);
    }
}
