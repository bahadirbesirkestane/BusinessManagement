using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanManageSettings)]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> TaskMail(CancellationToken cancellationToken)
    {
        var settings = await _context.TaskEmailNotificationSettings
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Ayarlar"] = null,
            ["Görev Mail Ayarları"] = null
        };

        return View(new TaskEmailSettingsViewModel
        {
            RecipientEmails = settings?.RecipientEmails
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaskMail(TaskEmailSettingsViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Ayarlar"] = null,
            ["Görev Mail Ayarları"] = null
        };

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var settings = await _context.TaskEmailNotificationSettings
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new TaskEmailNotificationSetting();
            _context.TaskEmailNotificationSettings.Add(settings);
        }

        settings.RecipientEmails = model.RecipientEmails?.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Görev mail ayarları kaydedildi.";
        return RedirectToAction(nameof(TaskMail));
    }
}
