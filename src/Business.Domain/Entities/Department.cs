using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class Department : BaseEntity
{
    [Required(ErrorMessage = "Departman adı zorunludur.")]
    [StringLength(160, ErrorMessage = "Departman adı en fazla 160 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
