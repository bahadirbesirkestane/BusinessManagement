using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class MaterialCategoryDefinition : BaseEntity
{
    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(120, ErrorMessage = "Kategori adı en fazla 120 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
