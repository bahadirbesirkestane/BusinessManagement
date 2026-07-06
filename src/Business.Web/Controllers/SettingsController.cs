using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanManageSettings)]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly TelegramBotOptions _telegramBotOptions;
    private readonly ITelegramNotificationService _telegramNotificationService;

    public SettingsController(
        ApplicationDbContext context,
        IOptions<TelegramBotOptions> telegramBotOptions,
        ITelegramNotificationService telegramNotificationService)
    {
        _context = context;
        _telegramBotOptions = telegramBotOptions.Value;
        _telegramNotificationService = telegramNotificationService;
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
    [HttpGet]
    public async Task<IActionResult> Telegram(CancellationToken cancellationToken)
    {
        var settings = await _context.TelegramNotificationSettings
            .Include(x => x.Recipients)
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Ayarlar"] = null,
            ["Telegram Ayarları"] = null
        };

        return View(await CreateTelegramSettingsViewModelAsync(settings, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Telegram(TelegramSettingsViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Ayarlar"] = null,
            ["Telegram Ayarları"] = null
        };

        model.IsBotTokenConfigured = !string.IsNullOrWhiteSpace(_telegramBotOptions.Token);
        model.IsWebhookSecretConfigured = !string.IsNullOrWhiteSpace(_telegramBotOptions.WebhookSecret);

        if (!ModelState.IsValid)
        {
            model.AvailableRecipients = await GetTelegramRecipientOptionsAsync(cancellationToken);
            return View(model);
        }

        var settings = await _context.TelegramNotificationSettings
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new TelegramNotificationSetting();
            _context.TelegramNotificationSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        settings.IsEnabled = model.IsEnabled;
        settings.BotUserName = string.IsNullOrWhiteSpace(model.BotUserName)
            ? null
            : model.BotUserName.Trim().TrimStart('@');
        settings.LinkCodeTtlMinutes = model.LinkCodeTtlMinutes;
        await _context.SaveChangesAsync(cancellationToken);

        await SyncTelegramRecipientsAsync(settings.Id, TelegramNotificationModule.MaterialRequest, model.MaterialRequestRecipientUserIds, cancellationToken);
        await SyncTelegramRecipientsAsync(settings.Id, TelegramNotificationModule.PurchaseOrder, model.PurchaseOrderRecipientUserIds, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Telegram ayarları kaydedildi.";
        return RedirectToAction(nameof(Telegram));
    }

    [HttpGet]
    public async Task<IActionResult> TelegramMessage(CancellationToken cancellationToken)
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Ayarlar"] = null,
            ["Telegram Mesaj Gönder"] = null
        };

        return View(await CreateTelegramMessageViewModelAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TelegramMessage(TelegramMessageViewModel model, CancellationToken cancellationToken)
    {
        ViewBag.Breadcrumbs = new Dictionary<string, string?>
        {
            ["Ayarlar"] = null,
            ["Telegram Mesaj Gönder"] = null
        };

        if (model.SelectedUserIds.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Lütfen en az bir kullanıcı seçin.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRecipients = (await CreateTelegramMessageViewModelAsync(cancellationToken)).AvailableRecipients;
            return View(model);
        }

        var result = await _telegramNotificationService.SendMessageToUsersAsync(
            model.SelectedUserIds,
            model.Message.Trim(),
            includeUsersWithNotificationsDisabled: true,
            cancellationToken);
        if (!result.IsEnabled)
        {
            TempData["Error"] = "Telegram bildirim sistemi kapalı olduğu için mesaj gönderilemedi.";
            return RedirectToAction(nameof(TelegramMessage));
        }

        if (!result.IsConfigured)
        {
            TempData["Error"] = "Telegram bot token ayarlanmadığı için mesaj gönderilemedi.";
            return RedirectToAction(nameof(TelegramMessage));
        }

        var notices = new List<string>
        {
            $"{result.SentRecipientCount} kullanıcıya Telegram mesajı gönderildi."
        };

        if (result.MissingChatRecipients.Count > 0)
        {
            notices.Add($"Bağlı Telegram hesabı olmayan kullanıcılar: {string.Join(", ", result.MissingChatRecipients)}");
        }

        if (result.FailedRecipients.Count > 0)
        {
            notices.Add($"Gönderim hatası alınan kullanıcılar: {string.Join(", ", result.FailedRecipients)}");
        }

        TempData[result.FailedRecipients.Count > 0 || result.MissingChatRecipients.Count > 0 ? "Error" : "Success"] = string.Join(" ", notices);
        return RedirectToAction(nameof(TelegramMessage));
    }

    private async Task<TelegramMessageViewModel> CreateTelegramMessageViewModelAsync(CancellationToken cancellationToken)
    {
        var recipients = await _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.TelegramChatId != null)
            .OrderBy(x => x.FullName)
            .Select(x => new TelegramMessageRecipientViewModel
            {
                UserId = x.Id,
                DisplayName = x.FullName ?? x.Email ?? x.UserName ?? x.Id,
                TelegramUserName = x.TelegramUsername,
                HasLinkedChat = x.TelegramChatId != null,
                NotificationsEnabled = x.TelegramNotificationsEnabled
            })
            .ToListAsync(cancellationToken);

        return new TelegramMessageViewModel
        {
            AvailableRecipients = recipients
        };
    }

    private async Task<TelegramSettingsViewModel> CreateTelegramSettingsViewModelAsync(TelegramNotificationSetting? settings, CancellationToken cancellationToken)
    {
        return new TelegramSettingsViewModel
        {
            IsEnabled = settings?.IsEnabled ?? false,
            BotUserName = settings?.BotUserName,
            LinkCodeTtlMinutes = settings?.LinkCodeTtlMinutes ?? 15,
            IsBotTokenConfigured = !string.IsNullOrWhiteSpace(_telegramBotOptions.Token),
            IsWebhookSecretConfigured = !string.IsNullOrWhiteSpace(_telegramBotOptions.WebhookSecret),
            MaterialRequestRecipientUserIds = settings?.Recipients
                .Where(x => x.Module == TelegramNotificationModule.MaterialRequest)
                .Select(x => x.UserId)
                .ToList() ?? [],
            PurchaseOrderRecipientUserIds = settings?.Recipients
                .Where(x => x.Module == TelegramNotificationModule.PurchaseOrder)
                .Select(x => x.UserId)
                .ToList() ?? [],
            AvailableRecipients = await GetTelegramRecipientOptionsAsync(cancellationToken)
        };
    }

    private async Task<List<TelegramMessageRecipientViewModel>> GetTelegramRecipientOptionsAsync(CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new TelegramMessageRecipientViewModel
            {
                UserId = x.Id,
                DisplayName = x.FullName ?? x.Email ?? x.UserName ?? x.Id,
                TelegramUserName = x.TelegramUsername,
                HasLinkedChat = x.TelegramChatId != null,
                NotificationsEnabled = x.TelegramNotificationsEnabled
            })
            .ToListAsync(cancellationToken);
    }

    private async Task SyncTelegramRecipientsAsync(
        Guid telegramNotificationSettingId,
        TelegramNotificationModule module,
        IEnumerable<string> selectedUserIds,
        CancellationToken cancellationToken)
    {
        var selectedIds = selectedUserIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToHashSet(StringComparer.Ordinal);

        var existingRecipients = await _context.TelegramNotificationRecipients
            .Where(x => x.TelegramNotificationSettingId == telegramNotificationSettingId && x.Module == module)
            .ToListAsync(cancellationToken);

        _context.TelegramNotificationRecipients.RemoveRange(existingRecipients.Where(x => !selectedIds.Contains(x.UserId)));

        var existingUserIds = existingRecipients
            .Select(x => x.UserId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var userId in selectedIds.Where(x => !existingUserIds.Contains(x)))
        {
            _context.TelegramNotificationRecipients.Add(new TelegramNotificationRecipient
            {
                TelegramNotificationSettingId = telegramNotificationSettingId,
                Module = module,
                UserId = userId
            });
        }
    }
}
