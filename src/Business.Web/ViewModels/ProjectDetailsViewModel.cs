using Business.Domain.Entities;

namespace Business.Web.ViewModels;

public class ProjectDetailsViewModel
{
    public Project Project { get; set; } = null!;
    public bool CanViewProductionUpdates { get; set; }
    public bool CanViewPurchasing { get; set; }
    public RecordActivityViewModel? Activity { get; set; }
}
