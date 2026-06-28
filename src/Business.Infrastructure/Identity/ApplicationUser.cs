using Microsoft.AspNetCore.Identity;

namespace Business.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid? DepartmentId { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ThemePreference { get; set; } = AppThemes.Current;
}
