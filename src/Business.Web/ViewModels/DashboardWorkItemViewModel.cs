namespace Business.Web.ViewModels;

public class DashboardWorkItemViewModel
{
    public string Module { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusCss { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = "Details";
    public Guid Id { get; set; }
}
