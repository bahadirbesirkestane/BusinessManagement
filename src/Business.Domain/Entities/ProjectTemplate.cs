using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class ProjectTemplate : BaseEntity
{
    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    [StringLength(180, ErrorMessage = "Şablon adı en fazla 180 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Şablon kodu en fazla 40 karakter olabilir.")]
    public string? Code { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ProjectTemplateTask> Tasks { get; set; } = new List<ProjectTemplateTask>();
}
