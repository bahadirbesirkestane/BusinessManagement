using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class TelegramMessageViewModel
{
    [Required(ErrorMessage = "Mesaj içeriği zorunludur.")]
    [StringLength(4000, ErrorMessage = "Mesaj içeriği en fazla 4000 karakter olabilir.")]
    [Display(Name = "Mesaj içeriği")]
    public string Message { get; set; } = string.Empty;

    public List<string> SelectedUserIds { get; set; } = [];
    public List<TelegramMessageRecipientViewModel> AvailableRecipients { get; set; } = [];
}

public class TelegramMessageRecipientViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? TelegramUserName { get; set; }
    public bool NotificationsEnabled { get; set; }
}
