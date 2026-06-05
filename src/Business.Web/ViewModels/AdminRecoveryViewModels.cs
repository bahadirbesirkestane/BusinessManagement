using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class GenerateAdminRecoveryCodesViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "1 ile 5 arasında kod üretilebilir.")]
    [Display(Name = "Kod Sayısı")]
    public int Count { get; set; } = 2;

    [Display(Name = "Geçerlilik Bitişi")]
    public DateTime? ExpiresAt { get; set; }
}

public class GeneratedAdminRecoveryCodesViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Codes { get; set; } = [];
}

public class AdminRecoveryResetViewModel
{
    [Required(ErrorMessage = "Admin e-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "Admin E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kurtarma kodu zorunludur.")]
    [Display(Name = "Kurtarma Kodu")]
    public string RecoveryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni parola zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Parola")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Parola tekrarı eşleşmiyor.")]
    [Display(Name = "Yeni Parola Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
