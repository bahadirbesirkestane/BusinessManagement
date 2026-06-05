using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class TaskCategory : BaseEntity
{
    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(120, ErrorMessage = "Kategori adı en fazla 120 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(24, ErrorMessage = "Renk değeri en fazla 24 karakter olabilir.")]
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
}
