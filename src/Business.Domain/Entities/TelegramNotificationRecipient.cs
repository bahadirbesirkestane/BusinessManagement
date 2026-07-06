using Business.Domain.Common;
using Business.Domain.Enums;

namespace Business.Domain.Entities;

public class TelegramNotificationRecipient : BaseEntity
{
    public Guid TelegramNotificationSettingId { get; set; }
    public TelegramNotificationSetting? TelegramNotificationSetting { get; set; }

    public TelegramNotificationModule Module { get; set; }

    public string UserId { get; set; } = string.Empty;
}
