namespace Business.Web.ViewModels;

public class DashboardReviewTaskViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string AssignedText { get; set; } = string.Empty;
    public string PriorityText { get; set; } = string.Empty;
    public string PriorityCss { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? SubmittedForReviewAt { get; set; }
}
