using System.Security.Claims;
using Business.Infrastructure.Identity;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanManageUsers)]
public class RolesController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _roleManager.Roles.OrderBy(x => x.Name).ToListAsync());
    }

    public IActionResult Create()
    {
        return View("Edit", new RolePermissionViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RolePermissionViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.RoleName))
        {
            ModelState.AddModelError(nameof(model.RoleName), "Rol adı zorunludur.");
            return View("Edit", model);
        }

        var role = new IdentityRole(model.RoleName.Trim());
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View("Edit", model);
        }

        await _roleManager.AddClaimAsync(role, new Claim(AppClaimTypes.PermissionSeeded, "true"));
        foreach (var permission in model.SelectedPermissions.Distinct())
        {
            await _roleManager.AddClaimAsync(role, new Claim(AppClaimTypes.Permission, permission));
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        return View(new RolePermissionViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name ?? string.Empty,
            SelectedPermissions = claims
                .Where(x => x.Type == AppClaimTypes.Permission)
                .Select(x => x.Value)
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, RolePermissionViewModel model)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        role.Name = model.RoleName.Trim();
        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            AddErrors(updateResult);
            return View(model);
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existingClaims.Where(x => x.Type == AppClaimTypes.Permission))
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var permission in model.SelectedPermissions.Distinct())
        {
            await _roleManager.AddClaimAsync(role, new Claim(AppClaimTypes.Permission, permission));
        }

        await RefreshUsersInRoleAsync(role.Name);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Count > 0)
        {
            TempData["Error"] = "Bu role bağlı kullanıcı var. Önce kullanıcıların rolünü kaldırın.";
            return RedirectToAction(nameof(Index));
        }

        await _roleManager.DeleteAsync(role);
        return RedirectToAction(nameof(Index));
    }

    private async Task RefreshUsersInRoleAsync(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        var users = await _userManager.GetUsersInRoleAsync(roleName);
        foreach (var user in users)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
