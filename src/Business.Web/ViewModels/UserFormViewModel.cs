using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(160, ErrorMessage = "Ad soyad en fazla 160 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(256, ErrorMessage = "E-posta en fazla 256 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "Telefon en fazla 80 karakter olabilir.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Departman")]
    public Guid? DepartmentId { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Parola 8 ile 100 karakter arasında olmalıdır.")]
    [Display(Name = "Parola")]
    public string? Password { get; set; }

    public List<string> SelectedRoles { get; set; } = [];
    public List<string> AvailableRoles { get; set; } = [];
    public List<string> SelectedPermissions { get; set; } = [];
}
