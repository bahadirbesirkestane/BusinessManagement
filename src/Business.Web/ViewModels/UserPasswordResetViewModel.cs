using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class UserPasswordResetViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni parola zorunludur.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Yeni parola 8 ile 100 karakter arasında olmalıdır.")]
    [Display(Name = "Yeni Parola")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Parola tekrarı eşleşmiyor.")]
    [Display(Name = "Yeni Parola Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
