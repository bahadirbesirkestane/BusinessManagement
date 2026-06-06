using System.Security.Claims;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Infrastructure.Seed;

public static class IdentitySeeder
{
    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppPermissionCatalog.RolePermissions.Keys)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        foreach (var (roleName, permissions) in AppPermissionCatalog.RolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var isSeeded = existingClaims.Any(x => x.Type == AppClaimTypes.PermissionSeeded);
            var existingPermissionClaims = existingClaims.Where(x => x.Type == AppClaimTypes.Permission).ToList();

            if (!isSeeded && existingPermissionClaims.Count == 0)
            {
                foreach (var permission in permissions)
                {
                    await roleManager.AddClaimAsync(role, new Claim(AppClaimTypes.Permission, permission));
                }
            }
            else if (isSeeded)
            {
                var existingPermissionValues = existingPermissionClaims.Select(x => x.Value).ToHashSet();
                foreach (var permission in permissions.Where(permission => !existingPermissionValues.Contains(permission)))
                {
                    await roleManager.AddClaimAsync(role, new Claim(AppClaimTypes.Permission, permission));
                }
            }

            if (!isSeeded)
            {
                await roleManager.AddClaimAsync(role, new Claim(AppClaimTypes.PermissionSeeded, "true"));
            }
        }

        var currentAdmins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        if (currentAdmins.Any(x => x.IsActive))
        {
            return;
        }

        var initialAdmin = GetInitialAdmin(configuration);
        if (initialAdmin is null)
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(initialAdmin.Email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = initialAdmin.Email,
                Email = initialAdmin.Email,
                EmailConfirmed = true,
                FullName = initialAdmin.FullName,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(admin, initialAdmin.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
            }
        }
        else if (!admin.IsActive)
        {
            admin.IsActive = true;
            var updateResult = await userManager.UpdateAsync(admin);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(x => x.Description)));
            }
        }
    }

    private static InitialAdminOptions? GetInitialAdmin(IConfiguration configuration)
    {
        var email = configuration["InitialAdmin:Email"] ?? configuration["SeedAdmin:Email"];
        var password = configuration["InitialAdmin:Password"] ?? configuration["SeedAdmin:Password"];
        var fullName = configuration["InitialAdmin:FullName"] ?? configuration["SeedAdmin:FullName"];
        var enabled = configuration.GetValue("InitialAdmin:Enabled", !string.IsNullOrWhiteSpace(email) || configuration.GetValue("SeedAdmin:Enabled", false));

        if (!enabled || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        if (IsPlaceholder(email) || IsPlaceholder(password))
        {
            return null;
        }

        return new InitialAdminOptions
        {
            Email = email.Trim(),
            Password = password.Trim(),
            FullName = string.IsNullOrWhiteSpace(fullName) ? "İlk Sistem Yöneticisi" : fullName.Trim()
        };
    }

    private static bool IsPlaceholder(string value)
    {
        return value.Contains("CHANGE_", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record InitialAdminOptions
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
    }
}
