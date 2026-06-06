using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class UserInviteViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(160, ErrorMessage = "Ad soyad en fazla 160 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(256, ErrorMessage = "E-posta en fazla 256 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seçimi zorunludur.")]
    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    public List<string> AvailableRoles { get; set; } = [];
}
