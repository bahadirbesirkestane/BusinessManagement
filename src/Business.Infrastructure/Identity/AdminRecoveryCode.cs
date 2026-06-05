using Business.Domain.Common;

namespace Business.Infrastructure.Identity;

public class AdminRecoveryCode : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = default!;
    public string CodeHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedIpAddress { get; set; }
    public string? Note { get; set; }

    public bool IsUsed => UsedAt.HasValue;
}
