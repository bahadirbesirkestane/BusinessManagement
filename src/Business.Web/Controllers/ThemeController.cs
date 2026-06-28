using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Business.Web.Controllers;

[Authorize]
public class ThemeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ThemeController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string theme, string? returnUrl)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        var normalizedTheme = AppThemes.NormalizeSelection(theme);
        if (!string.Equals(user.ThemePreference, normalizedTheme, StringComparison.Ordinal))
        {
            user.ThemePreference = normalizedTheme;
            await _userManager.UpdateAsync(user);
        }

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Dashboard");
    }
}
