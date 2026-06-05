using Business.Application.Services;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[AllowAnonymous]
public class AdminRecoveryController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAdminRecoveryCodeService _recoveryCodeService;

    public AdminRecoveryController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IAdminRecoveryCodeService recoveryCodeService)
    {
        _userManager = userManager;
        _context = context;
        _recoveryCodeService = recoveryCodeService;
    }

    public IActionResult Reset()
    {
        return View(new AdminRecoveryResetViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(AdminRecoveryResetViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            AddGenericError();
            return View(model);
        }

        var now = DateTime.UtcNow;
        var codes = await _context.AdminRecoveryCodes
            .Where(x => x.UserId == user.Id && x.UsedAt == null && (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var matchingCode = codes.FirstOrDefault(code =>
            _recoveryCodeService.VerifyCode(model.RecoveryCode, code.Salt, code.CodeHash));

        if (matchingCode is null)
        {
            AddGenericError();
            return View(model);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        matchingCode.UsedAt = now;
        matchingCode.UsedIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _context.SaveChangesAsync(cancellationToken);
        await _userManager.UpdateSecurityStampAsync(user);

        TempData["Success"] = "Admin şifresi yenilendi. Yeni şifreyle giriş yapabilirsiniz.";
        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }

    private void AddGenericError()
    {
        ModelState.AddModelError(string.Empty, "Kurtarma bilgileri doğrulanamadı.");
    }
}
