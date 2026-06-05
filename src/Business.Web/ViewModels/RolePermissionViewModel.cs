using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class RolePermissionViewModel
{
    public string RoleId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol adı zorunludur.")]
    [StringLength(120, ErrorMessage = "Rol adı en fazla 120 karakter olabilir.")]
    public string RoleName { get; set; } = string.Empty;
    public List<string> SelectedPermissions { get; set; } = [];
}
