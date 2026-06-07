using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Business.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class TwoFactorSetupModel : PageModel
{
    private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private readonly UserManager<ApplicationUser> _userManager;

    public TwoFactorSetupModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsAdmin { get; private set; }
    public bool IsTwoFactorEnabled { get; private set; }
    public string SharedKey { get; private set; } = string.Empty;
    public string AuthenticatorUri { get; private set; } = string.Empty;
    public int RecoveryCodesLeft { get; private set; }
    public IReadOnlyList<string> RecoveryCodes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        await LoadAsync(user);
        if (!IsAdmin)
        {
            ModelState.AddModelError(string.Empty, "Bu ekran yalnızca admin kullanıcılar içindir.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode);

        if (!isValid)
        {
            ModelState.AddModelError(nameof(Input.Code), "Doğrulama kodu geçersiz.");
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes = recoveryCodes?.ToList() ?? [];
        IsTwoFactorEnabled = true;
        RecoveryCodesLeft = RecoveryCodes.Count;
        TempData["Success"] = "İki aşamalı doğrulama etkinleştirildi. Kurtarma kodlarını güvenli bir yerde saklayın.";
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        if (!await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            return Forbid();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        TempData["Success"] = "Authenticator ayarı sıfırlandı. Lütfen yeniden kurulum yapın.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        IsAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        SharedKey = FormatKey(key ?? string.Empty);
        AuthenticatorUri = GenerateQrCodeUri(user.Email ?? user.UserName ?? user.Id, key ?? string.Empty);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private static string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return string.Format(
            AuthenticatorUriFormat,
            UrlEncoder.Default.Encode("FirmaTakip"),
            UrlEncoder.Default.Encode(email),
            unformattedKey);
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [StringLength(10, MinimumLength = 6, ErrorMessage = "Doğrulama kodu en az 6 karakter olmalıdır.")]
        [Display(Name = "Authenticator Kodu")]
        public string Code { get; set; } = string.Empty;
    }
}
