namespace Business.Web.ViewModels;

public class DashboardWorkspaceViewModel
{
    public int OpenTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int DueTodayCount { get; set; }
    public int WaitingReviewCount { get; set; }
    public bool CanViewReviewQueue { get; set; }
    public int ReviewQueueCount { get; set; }
    public IReadOnlyList<DashboardUserTaskViewModel> Tasks { get; set; } = [];
    public IReadOnlyList<DashboardReviewTaskViewModel> ReviewQueueTasks { get; set; } = [];
}
