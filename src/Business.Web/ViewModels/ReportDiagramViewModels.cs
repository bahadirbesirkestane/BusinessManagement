using Business.Domain.Enums;

namespace Business.Web.ViewModels;

public sealed class GanttReportViewModel
{
    public DateTime TimelineStart { get; set; }
    public DateTime TimelineEnd { get; set; }
    public IReadOnlyList<GanttTimelineSegmentViewModel> Segments { get; set; } = [];
    public IReadOnlyList<GanttTimelineSegmentViewModel> DaySegments { get; set; } = [];
    public IReadOnlyList<GanttRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<ProjectFilterOptionViewModel> Projects { get; set; } = [];
    public Guid? ProjectId { get; set; }
    public string TimelineScaleText { get; set; } = string.Empty;
    public int ActiveProjectCount { get; set; }
    public int TaskCount { get; set; }
    public int LateTaskCount { get; set; }
}

public sealed class GanttTimelineSegmentViewModel
{
    public string Label { get; set; } = string.Empty;
    public double OffsetPercent { get; set; }
    public double WidthPercent { get; set; }
}

public sealed class GanttRowViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusCss { get; set; } = string.Empty;
    public string PriorityCss { get; set; } = string.Empty;
    public string PriorityText { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ProgressPercent { get; set; }
    public double OffsetPercent { get; set; }
    public double WidthPercent { get; set; }
    public bool IsProject { get; set; }
    public bool IsLate { get; set; }
    public string? Url { get; set; }
}

public sealed class KanbanReportViewModel
{
    public IReadOnlyList<KanbanColumnViewModel> Columns { get; set; } = [];
    public IReadOnlyList<ProjectFilterOptionViewModel> Projects { get; set; } = [];
    public Guid? ProjectId { get; set; }
    public int TotalTaskCount { get; set; }
    public int LateTaskCount { get; set; }
    public int ReviewTaskCount { get; set; }
}

public sealed class KanbanColumnViewModel
{
    public WorkTaskStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StatusCss { get; set; } = string.Empty;
    public IReadOnlyList<KanbanTaskCardViewModel> Tasks { get; set; } = [];
}

public sealed class KanbanTaskCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProjectText { get; set; } = string.Empty;
    public string CategoryText { get; set; } = string.Empty;
    public string ResponsibleText { get; set; } = string.Empty;
    public string AssignedText { get; set; } = string.Empty;
    public string PriorityText { get; set; } = string.Empty;
    public string PriorityCss { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsLate { get; set; }
}

public sealed class ProjectFilterOptionViewModel
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
