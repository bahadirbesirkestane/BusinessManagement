using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Business.Web.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public string Email { get; private set; } = string.Empty;
    public bool EmailConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public bool IsAdmin { get; private set; }
    public string CurrentTheme { get; private set; } = AppThemes.Current;

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        Email = user.Email ?? string.Empty;
        EmailConfirmed = user.EmailConfirmed;
        TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        IsAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        CurrentTheme = AppThemes.ResolveForRender(user.ThemePreference, isAuthenticated: true);
        return Page();
    }
}
