using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class ProjectTemplateTask : BaseEntity
{
    public Guid ProjectTemplateId { get; set; }
    public ProjectTemplate ProjectTemplate { get; set; } = null!;

    public Guid? ParentTemplateTaskId { get; set; }
    public ProjectTemplateTask? ParentTemplateTask { get; set; }

    [Required(ErrorMessage = "Görev başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Görev başlığı en fazla 220 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    [StringLength(40, ErrorMessage = "WBS kodu en fazla 40 karakter olabilir.")]
    public string? WbsCode { get; set; }

    public int OutlineLevel { get; set; }
    public Guid? TaskCategoryId { get; set; }
    public TaskCategory? TaskCategory { get; set; }
    public int? DefaultDurationDays { get; set; }
    public int? DefaultStartOffsetDays { get; set; }
    public ProjectPriority DefaultPriority { get; set; } = ProjectPriority.Normal;
    public string? DefaultAssignedUserId { get; set; }
    public string? DefaultResponsibleUserId { get; set; }
    public bool IsMilestone { get; set; }

    public ICollection<ProjectTemplateTask> SubTasks { get; set; } = new List<ProjectTemplateTask>();
}
