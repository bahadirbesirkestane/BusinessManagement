using Business.Infrastructure.Identity;
using Business.Infrastructure.Data;
using Business.Application.Services;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewUsers)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAdminRecoveryCodeService _recoveryCodeService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IAdminRecoveryCodeService recoveryCodeService,
        IEmailSender emailSender,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _recoveryCodeService = recoveryCodeService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(x => x.FullName).ToListAsync();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                RolesText = roles.Count == 0 ? "-" : string.Join(", ", roles)
            });
        }

        return View(model);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        return View(new UserInviteViewModel { AvailableRoles = await GetRoleNamesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(UserInviteViewModel model, CancellationToken cancellationToken)
    {
        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Geçerli bir rol seçin.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRoleNamesAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = false,
            FullName = model.FullName.Trim(),
            IsActive = false
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.AvailableRoles = await GetRoleNamesAsync();
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            model.AvailableRoles = await GetRoleNamesAsync();
            return View(model);
        }

        var inviteLink = await CreateInviteLinkAsync(user);
        try
        {
            await _emailSender.SendAsync(new EmailMessage
            {
                To = user.Email!,
                Subject = "FirmaTakip kullanıcı daveti",
                HtmlBody = $"""
                    <p>Merhaba {HtmlEncoder.Default.Encode(user.FullName ?? user.Email!)},</p>
                    <p>FirmaTakip sistemine erişiminiz için kullanıcı daveti oluşturuldu.</p>
                    <p><a href="{HtmlEncoder.Default.Encode(inviteLink)}">Hesabımı doğrula ve şifremi belirle</a></p>
                    <p>Bu bağlantı geçersiz veya süresi dolmuşsa sistem yöneticinizden yeni davet isteyin.</p>
                    """,
                TextBody = $"""
                    Merhaba {user.FullName ?? user.Email!},

                    FirmaTakip sistemine erişiminiz için kullanıcı daveti oluşturuldu.
                    Davet linki: {inviteLink}

                    Bu bağlantı geçersiz veya süresi dolmuşsa sistem yöneticinizden yeni davet isteyin.
                    """
            }, cancellationToken);

            TempData["Success"] = $"{user.Email} adresine kullanıcı daveti gönderildi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı daveti e-postası gönderilemedi. UserId: {UserId}", user.Id);
            TempData["Error"] = "Kullanıcı oluşturuldu ancak davet e-postası gönderilemedi. SMTP ayarlarını kontrol edin.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        ViewBag.Departments = await GetDepartmentsAsync();
        return View(new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            DepartmentId = user.DepartmentId,
            IsActive = user.IsActive,
            SelectedRoles = roles.ToList(),
            SelectedPermissions = claims.Where(x => x.Type == AppClaimTypes.Permission).Select(x => x.Value).ToList(),
            AvailableRoles = await GetRoleNamesAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "Kendi kullanıcınızı silemezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin) && !await CanApplyRoleChangesAsync(user, []))
        {
            TempData["Error"] = "Sistemde en az bir aktif yönetici kalmalıdır.";
            return RedirectToAction(nameof(Index));
        }

        if (await HasBusinessReferencesAsync(user.Id))
        {
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["Error"] = "Kullanıcı geçmiş kayıtlarda kullanıldığı için silinmedi, pasife alındı.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            TempData["Error"] = "Kullanıcı silinemedi. Kayıt ilişkileri kontrol edildi.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ResetTwoFactor(string id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null || !currentUser.IsActive || !await _userManager.IsInRoleAsync(currentUser, AppRoles.Admin))
        {
            return Forbid();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == currentUser.Id)
        {
            TempData["Error"] = "Kendi 2FA ayarınızı kullanıcı listesinden sıfırlayamazsınız. 2FA Ayarları ekranını kullanın.";
            return RedirectToAction(nameof(Index));
        }

        if (!user.IsActive || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            TempData["Error"] = "2FA sıfırlama yalnızca aktif admin kullanıcılar için yapılabilir.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        TempData["Success"] = $"{user.Email} için 2FA sıfırlandı. Kullanıcı bir sonraki girişte yeniden kurulum yapmalıdır.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.DepartmentId = model.DepartmentId;
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            return View(model);
        }

        if (!await CanApplyRoleChangesAsync(user, model.SelectedRoles))
        {
            ModelState.AddModelError(string.Empty, "Sistemde en az bir aktif yönetici kalmalıdır.");
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            return View(model);
        }

        await SyncRolesAsync(user, model.SelectedRoles);
        await SyncUserPermissionsAsync(user, model.SelectedPermissions);
        await _userManager.UpdateSecurityStampAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> ResetPassword(string id)
    {
        await Task.CompletedTask;
        TempData["Error"] = "Kullanıcı şifresi admin tarafından belirlenemez. Kullanıcı davet bağlantısı üzerinden kendi şifresini belirlemelidir.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> ResetPassword(string id, UserPasswordResetViewModel model)
    {
        await Task.CompletedTask;
        TempData["Error"] = "Kullanıcı şifresi admin tarafından belirlenemez. Kullanıcı davet bağlantısı üzerinden kendi şifresini belirlemelidir.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GenerateRecoveryCodes(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!user.IsActive || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            TempData["Error"] = "Kurtarma kodu yalnızca yönetici kullanıcılar için üretilebilir.";
            return RedirectToAction(nameof(Index));
        }

        return View(new GenerateAdminRecoveryCodesViewModel
        {
            UserId = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Count = 2,
            ExpiresAt = DateTime.Today.AddYears(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GenerateRecoveryCodes(string id, GenerateAdminRecoveryCodesViewModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        model.FullName = user.FullName ?? string.Empty;
        model.Email = user.Email ?? string.Empty;

        if (!user.IsActive || !await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            ModelState.AddModelError(string.Empty, "Kurtarma kodu yalnızca yönetici kullanıcılar için üretilebilir.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var plainCodes = _recoveryCodeService.GeneratePlainCodes(model.Count);
        foreach (var code in plainCodes)
        {
            var salt = _recoveryCodeService.CreateSalt();
            _context.AdminRecoveryCodes.Add(new AdminRecoveryCode
            {
                UserId = user.Id,
                Salt = salt,
                CodeHash = _recoveryCodeService.HashCode(code, salt),
                ExpiresAt = model.ExpiresAt,
                Note = "Admin panelinden üretildi"
            });
        }

        await _context.SaveChangesAsync();

        return View("GeneratedRecoveryCodes", new GeneratedAdminRecoveryCodesViewModel
        {
            FullName = model.FullName,
            Email = model.Email,
            Codes = plainCodes
        });
    }

    private async Task<List<string>> GetRoleNamesAsync()
    {
        return await _roleManager.Roles.OrderBy(x => x.Name).Select(x => x.Name!).ToListAsync();
    }

    private async Task<string> CreateInviteLinkAsync(ApplicationUser user)
    {
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var passwordToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedEmailToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailToken));
        var encodedPasswordToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(passwordToken));

        return Url.Page(
            "/Account/AcceptInvitation",
            pageHandler: null,
            values: new { area = "Identity", userId = user.Id, emailToken = encodedEmailToken, passwordToken = encodedPasswordToken },
            protocol: Request.Scheme) ?? string.Empty;
    }

    private async Task SyncRolesAsync(ApplicationUser user, List<string> selectedRoles)
    {
        var currentRoles = await _userManager.GetRolesAsync(user);
        var selected = selectedRoles.Distinct().ToList();
        await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(selected));
        await _userManager.AddToRolesAsync(user, selected.Except(currentRoles));
    }

    private async Task<bool> CanApplyRoleChangesAsync(ApplicationUser targetUser, List<string> selectedRoles)
    {
        var keepsAdminRole = selectedRoles.Contains(AppRoles.Admin);
        if (keepsAdminRole && targetUser.IsActive)
        {
            return true;
        }

        var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
        return admins.Any(user => user.Id != targetUser.Id && user.IsActive);
    }

    private async Task SyncUserPermissionsAsync(ApplicationUser user, List<string> selectedPermissions)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in claims.Where(x => x.Type == AppClaimTypes.Permission))
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        foreach (var permission in selectedPermissions.Distinct())
        {
            await _userManager.AddClaimAsync(user, new Claim(AppClaimTypes.Permission, permission));
        }
    }

    private async Task<List<Business.Domain.Entities.Department>> GetDepartmentsAsync()
    {
        return await _context.Departments.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    private async Task<bool> HasBusinessReferencesAsync(string userId)
    {
        return await _context.ProjectTasks.AnyAsync(x =>
                   x.AssignedToUserId == userId ||
                   x.ResponsibleUserId == userId ||
                   x.Assignments.Any(assignment => assignment.UserId == userId)) ||
               await _context.MaterialRequests.AnyAsync(x => x.RequestedByUserId == userId) ||
               await _context.RecordComments.AnyAsync(x => x.CreatedByUserId == userId) ||
               await _context.RecordFiles.AnyAsync(x => x.CreatedByUserId == userId);
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
