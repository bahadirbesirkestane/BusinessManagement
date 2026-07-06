using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class TelegramNotificationSetting : BaseEntity
{
    public bool IsEnabled { get; set; }

    [StringLength(128)]
    public string? BotUserName { get; set; }

    [Range(1, 1440)]
    public int LinkCodeTtlMinutes { get; set; } = 15;

    public ICollection<TelegramNotificationRecipient> Recipients { get; set; } = new List<TelegramNotificationRecipient>();
}
