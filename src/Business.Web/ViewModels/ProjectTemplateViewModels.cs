using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class ProjectTemplateListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int TaskCount { get; set; }
}

public class ProjectTemplateFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    [StringLength(180, ErrorMessage = "Şablon adı en fazla 180 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Şablon kodu en fazla 40 karakter olabilir.")]
    public string? Code { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ProjectTemplateTaskFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }
    public Guid ProjectTemplateId { get; set; }
    public Guid? ParentTemplateTaskId { get; set; }

    [Required(ErrorMessage = "Görev başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Görev başlığı en fazla 220 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public Guid? TaskCategoryId { get; set; }
    public int? DefaultDurationDays { get; set; }
    public int? DefaultStartOffsetDays { get; set; }
    public ProjectPriority DefaultPriority { get; set; } = ProjectPriority.Normal;
    public string? DefaultAssignedUserId { get; set; }
    public string? DefaultResponsibleUserId { get; set; }
    public bool IsMilestone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DefaultDurationDays.HasValue && DefaultDurationDays.Value < 1)
        {
            yield return new ValidationResult("Varsayılan süre en az 1 gün olmalıdır.", [nameof(DefaultDurationDays)]);
        }
    }
}

public class ProjectTemplateTaskItemViewModel
{
    public Guid Id { get; set; }
    public Guid? ParentTemplateTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OutlineLevel { get; set; }
    public int SortOrder { get; set; }
    public string WbsCode { get; set; } = string.Empty;
    public Guid? TaskCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? DefaultDurationDays { get; set; }
    public int? DefaultStartOffsetDays { get; set; }
    public ProjectPriority DefaultPriority { get; set; }
    public string PriorityText { get; set; } = string.Empty;
    public string? DefaultAssignedUserId { get; set; }
    public string? AssignedUserText { get; set; }
    public string? DefaultResponsibleUserId { get; set; }
    public string? ResponsibleUserText { get; set; }
    public bool IsMilestone { get; set; }
    public bool HasChildren { get; set; }
}

public class ProjectTemplateLookupItemViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ProjectTemplateApplyViewModel
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required(ErrorMessage = "Proje seçimi zorunludur.")]
    public Guid ProjectId { get; set; }

    public DateTime? BaseStartDate { get; set; }
    public string? ReturnAction { get; set; }
}

public class ProjectTemplateDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<ProjectTemplateTaskItemViewModel> Tasks { get; set; } = [];
    public ProjectTemplateTaskFormViewModel TaskForm { get; set; } = new();
    public IReadOnlyList<ProjectTemplateLookupItemViewModel> TaskCategories { get; set; } = [];
    public IReadOnlyList<ProjectTemplateLookupItemViewModel> Users { get; set; } = [];
    public IReadOnlyList<ProjectPlanningProjectOptionViewModel> Projects { get; set; } = [];
    public ProjectTemplateApplyViewModel ApplyForm { get; set; } = new();
    public bool OpenTaskForm { get; set; }
    public string TaskFormMode { get; set; } = "create";
}
