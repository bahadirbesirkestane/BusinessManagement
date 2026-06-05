using Business.Infrastructure.Identity;
using Business.Infrastructure.Data;
using Business.Application.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewUsers)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAdminRecoveryCodeService _recoveryCodeService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IAdminRecoveryCodeService recoveryCodeService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _recoveryCodeService = recoveryCodeService;
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
                RolesText = roles.Count == 0 ? "-" : string.Join(", ", roles)
            });
        }

        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await GetDepartmentsAsync();
        return View(new UserFormViewModel { AvailableRoles = await GetRoleNamesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Yeni kullanıcı için parola zorunludur.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber,
            DepartmentId = model.DepartmentId,
            IsActive = model.IsActive
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.AvailableRoles = await GetRoleNamesAsync();
            ViewBag.Departments = await GetDepartmentsAsync();
            return View(model);
        }

        await SyncRolesAsync(user, model.SelectedRoles);
        await SyncUserPermissionsAsync(user, model.SelectedPermissions);
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

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!passwordResult.Succeeded)
            {
                AddIdentityErrors(passwordResult);
                model.AvailableRoles = await GetRoleNamesAsync();
                ViewBag.Departments = await GetDepartmentsAsync();
                return View(model);
            }
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
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new UserPasswordResetViewModel
        {
            UserId = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageUsers)]
    public async Task<IActionResult> ResetPassword(string id, UserPasswordResetViewModel model)
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

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await _userManager.UpdateSecurityStampAsync(user);
        TempData["Success"] = $"{model.Email} kullanıcısının şifresi sıfırlandı.";
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
