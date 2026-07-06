using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class TelegramSettingsViewModel
{
    [Display(Name = "Telegram bildirimleri aktif")]
    public bool IsEnabled { get; set; }

    [StringLength(128, ErrorMessage = "Bot kullanıcı adı en fazla 128 karakter olabilir.")]
    [Display(Name = "Bot kullanıcı adı")]
    public string? BotUserName { get; set; }

    [Range(1, 1440, ErrorMessage = "Bağlama kodu geçerlilik süresi 1 ile 1440 dakika arasında olmalıdır.")]
    [Display(Name = "Bağlama kodu geçerlilik süresi (dakika)")]
    public int LinkCodeTtlMinutes { get; set; } = 15;

    public bool IsBotTokenConfigured { get; set; }
    public bool IsWebhookSecretConfigured { get; set; }
}
