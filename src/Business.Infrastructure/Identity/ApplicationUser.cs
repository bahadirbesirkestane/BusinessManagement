using Microsoft.AspNetCore.Identity;

namespace Business.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid? DepartmentId { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ThemePreference { get; set; } = AppThemes.Current;
    public string? TelegramChatId { get; set; }
    public string? TelegramUsername { get; set; }
    public DateTime? TelegramLinkedAt { get; set; }
    public bool TelegramNotificationsEnabled { get; set; }
}
