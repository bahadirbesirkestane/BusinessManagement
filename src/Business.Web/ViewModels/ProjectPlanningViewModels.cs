using Business.Domain.Enums;

namespace Business.Web.ViewModels;

public class ProjectPlanningIndexViewModel
{
    public Guid? ProjectId { get; set; }
    public string? SelectedProjectText { get; set; }
    public IReadOnlyList<ProjectPlanningProjectOptionViewModel> Projects { get; set; } = [];
    public IReadOnlyList<ProjectPlanningTaskRowViewModel> Tasks { get; set; } = [];
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
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsMilestone { get; set; }
    public bool HasChildren { get; set; }
}
