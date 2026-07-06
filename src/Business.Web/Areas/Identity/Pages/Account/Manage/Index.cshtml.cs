using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Business.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly ITelegramLinkService _telegramLinkService;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        ITelegramLinkService telegramLinkService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _telegramLinkService = telegramLinkService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string Email { get; private set; } = string.Empty;
    public bool EmailConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public bool IsAdmin { get; private set; }
    public string CurrentTheme { get; private set; } = AppThemes.Current;
    public bool TelegramSettingsEnabled { get; private set; }
    public string? TelegramBotUserName { get; private set; }
    public string? TelegramChatId { get; private set; }
    public string? TelegramUsername { get; private set; }
    public DateTime? TelegramLinkedAt { get; private set; }
    public string? ActiveTelegramLinkCode { get; private set; }
    public DateTime? ActiveTelegramLinkExpiresAt { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        await LoadAsync(user, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveProfileAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user, cancellationToken);
            return Page();
        }

        user.PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim();
        user.TelegramNotificationsEnabled = Input.TelegramNotificationsEnabled;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user, cancellationToken);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Hesap iletişim ayarları kaydedildi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateTelegramLinkAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        var settings = await GetTelegramSettingsAsync(cancellationToken);
        if (settings is null || !settings.IsEnabled || string.IsNullOrWhiteSpace(settings.BotUserName))
        {
            StatusMessage = "Telegram bağlama kodu üretilemedi. Lütfen sistem yöneticinizle görüşün.";
            return RedirectToPage();
        }

        await _telegramLinkService.CreateOrRefreshAsync(user.Id, settings.LinkCodeTtlMinutes, cancellationToken);
        StatusMessage = "Telegram bağlama kodu üretildi.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var settings = await GetTelegramSettingsAsync(cancellationToken);
        var activeLink = await _telegramLinkService.GetActiveRequestAsync(user.Id, cancellationToken);

        Email = user.Email ?? string.Empty;
        EmailConfirmed = user.EmailConfirmed;
        TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        IsAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        CurrentTheme = AppThemes.ResolveForRender(user.ThemePreference, isAuthenticated: true);
        TelegramSettingsEnabled = settings?.IsEnabled ?? false;
        TelegramBotUserName = settings?.BotUserName;
        TelegramChatId = user.TelegramChatId;
        TelegramUsername = user.TelegramUsername;
        TelegramLinkedAt = user.TelegramLinkedAt;
        ActiveTelegramLinkCode = activeLink?.Code;
        ActiveTelegramLinkExpiresAt = activeLink?.ExpiresAt;

        Input = new InputModel
        {
            PhoneNumber = user.PhoneNumber,
            TelegramNotificationsEnabled = user.TelegramNotificationsEnabled
        };
    }

    private Task<Business.Domain.Entities.TelegramNotificationSetting?> GetTelegramSettingsAsync(CancellationToken cancellationToken)
    {
        return _context.TelegramNotificationSettings
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public class InputModel
    {
        [StringLength(80, ErrorMessage = "Telefon en fazla 80 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Telegram bildirimlerini aktif tut")]
        public bool TelegramNotificationsEnabled { get; set; }
    }
}
