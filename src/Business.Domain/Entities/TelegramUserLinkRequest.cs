using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class TelegramUserLinkRequest : BaseEntity
{
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [StringLength(64)]
    public string? TelegramChatId { get; set; }

    [StringLength(128)]
    public string? TelegramUsername { get; set; }
}
