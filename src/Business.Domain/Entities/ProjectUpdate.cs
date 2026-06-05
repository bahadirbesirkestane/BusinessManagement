using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class ProjectUpdate : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required(ErrorMessage = "Güncelleme başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Güncelleme başlığı en fazla 220 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }
}
