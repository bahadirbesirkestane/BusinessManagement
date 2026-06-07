using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Business.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class TwoFactorAuthenticationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public TwoFactorAuthenticationModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public bool IsAdmin { get; private set; }
    public bool HasAuthenticator { get; private set; }
    public bool IsTwoFactorEnabled { get; private set; }
    public int RecoveryCodesLeft { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        IsAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        HasAuthenticator = !string.IsNullOrWhiteSpace(authenticatorKey);
        IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        return Page();
    }
}
