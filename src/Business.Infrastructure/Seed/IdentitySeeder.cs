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

        foreach (var adminSeed in GetAdminSeeds(configuration))
        {
            var admin = await userManager.FindByEmailAsync(adminSeed.Email);
            if (admin is null)
            {
                if (string.IsNullOrWhiteSpace(adminSeed.Password))
                {
                    continue;
                }

                admin = new ApplicationUser
                {
                    UserName = adminSeed.Email,
                    Email = adminSeed.Email,
                    EmailConfirmed = true,
                    FullName = string.IsNullOrWhiteSpace(adminSeed.FullName) ? "Sistem Yöneticisi" : adminSeed.FullName
                };

                var result = await userManager.CreateAsync(admin, adminSeed.Password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            }
        }
    }

    private static List<SeedAdminOptions> GetAdminSeeds(IConfiguration configuration)
    {
        if (!configuration.GetValue("SeedAdmin:Enabled", false))
        {
            return [];
        }

        var admins = configuration
            .GetSection("SeedAdmins")
            .Get<List<SeedAdminOptions>>()?
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .Select(x => x with
            {
                Email = x.Email.Trim(),
                Password = x.Password?.Trim(),
                FullName = x.FullName?.Trim()
            })
            .ToList() ?? [];

        var legacyEmail = configuration["SeedAdmin:Email"];
        if (!string.IsNullOrWhiteSpace(legacyEmail) &&
            admins.All(x => !string.Equals(x.Email, legacyEmail, StringComparison.OrdinalIgnoreCase)))
        {
            admins.Add(new SeedAdminOptions
            {
                Email = legacyEmail.Trim(),
                Password = configuration["SeedAdmin:Password"]?.Trim(),
                FullName = configuration["SeedAdmin:FullName"]?.Trim()
            });
        }

        return admins;
    }

    private sealed record SeedAdminOptions
    {
        public string Email { get; init; } = string.Empty;
        public string? Password { get; init; }
        public string? FullName { get; init; }
    }
}
