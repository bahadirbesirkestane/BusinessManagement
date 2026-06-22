namespace Business.Web.ViewModels;

public class DashboardUserTaskViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string RoleText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusCss { get; set; } = string.Empty;
    public string PriorityText { get; set; } = string.Empty;
    public string PriorityCss { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsOverdue { get; set; }
}
