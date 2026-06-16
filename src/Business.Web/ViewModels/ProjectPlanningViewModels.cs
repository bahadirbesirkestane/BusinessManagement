using System.ComponentModel.DataAnnotations;
using Business.Domain.Enums;

namespace Business.Web.ViewModels;

public class ProjectPlanningIndexViewModel
{
    public Guid? ProjectId { get; set; }
    public string? SelectedProjectText { get; set; }
    public ProjectPlanningSelectedProjectViewModel? SelectedProject { get; set; }
    public ProjectStatus? SelectedProjectStatus { get; set; }
    public string SelectedProjectStatusText { get; set; } = string.Empty;
    public string SelectedProjectStatusCss { get; set; } = string.Empty;
    public IReadOnlyList<ProjectPlanningProjectOptionViewModel> Projects { get; set; } = [];
    public IReadOnlyList<ProjectPlanningTaskRowViewModel> Tasks { get; set; } = [];
    public IReadOnlyList<ProjectPlanningGanttTaskViewModel> GanttTasks { get; set; } = [];
    public IReadOnlyList<ProjectPlanningUserOptionViewModel> Users { get; set; } = [];
    public IReadOnlyList<ProjectPlanningTemplateOptionViewModel> Templates { get; set; } = [];
    public ProjectPlanningTaskFormViewModel TaskForm { get; set; } = new();
    public ProjectTemplateApplyViewModel TemplateApplyForm { get; set; } = new();
    public bool OpenTaskForm { get; set; }
    public string TaskFormMode { get; set; } = "create";
}

public class ProjectPlanningSelectedProjectViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string CustomerText { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusCss { get; set; } = string.Empty;
    public string PriorityText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? TargetEndDate { get; set; }
    public int TaskCount { get; set; }
}

public class ProjectPlanningProjectOptionViewModel
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class ProjectPlanningTaskRowViewModel
{
    public Guid Id { get; set; }
    public Guid? ParentTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WbsCode { get; set; }
    public string DisplayWbsCode { get; set; } = string.Empty;
    public int OutlineLevel { get; set; }
    public int SortOrder { get; set; }
    public WorkTaskStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusCss { get; set; } = string.Empty;
    public ProjectPriority Priority { get; set; }
    public string PriorityText { get; set; } = string.Empty;
    public string PriorityCss { get; set; } = string.Empty;
    public string ResponsibleText { get; set; } = string.Empty;
    public string AssignedText { get; set; } = string.Empty;
    public IReadOnlyList<string> AssignedUserIds { get; set; } = [];
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsMilestone { get; set; }
    public bool HasChildren { get; set; }
    public string LatestUpdateText { get; set; } = string.Empty;
    public string? LatestUpdateDescription { get; set; }
    public DateTime? LatestUpdateAt { get; set; }
}

public class ProjectPlanningUserOptionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ProjectPlanningTemplateOptionViewModel
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class ProjectPlanningTaskFormViewModel : IValidatableObject
{
    public Guid? TaskId { get; set; }

    [Required(ErrorMessage = "Proje bilgisi zorunludur.")]
    public Guid ProjectId { get; set; }

    public Guid? ParentTaskId { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(220, ErrorMessage = "Başlık en fazla 220 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Todo;
    public ProjectPriority Priority { get; set; } = ProjectPriority.Normal;
    public RecordVisibility Visibility { get; set; } = RecordVisibility.General;
    public List<string> AssignedUserIds { get; set; } = [];

    [Range(0, 100, ErrorMessage = "İlerleme yüzdesi 0 ile 100 arasında olmalıdır.")]
    public int ProgressPercent { get; set; }

    public bool IsMilestone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && DueDate.HasValue && DueDate.Value.Date < StartDate.Value.Date)
        {
            yield return new ValidationResult("Termin tarihi başlangıç tarihinden önce olamaz.", [nameof(DueDate)]);
        }
    }
}

public class ProjectPlanningGanttTaskViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string Dependencies { get; set; } = string.Empty;
    public string CustomClass { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string PriorityText { get; set; } = string.Empty;
    public string AssignedText { get; set; } = string.Empty;
    public string DateRangeText { get; set; } = string.Empty;
    public string LatestUpdateText { get; set; } = string.Empty;
    public string? LatestUpdateDescription { get; set; }
    public string LatestUpdateAtText { get; set; } = string.Empty;
}
