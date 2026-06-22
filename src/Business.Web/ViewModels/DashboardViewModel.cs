using Business.Domain.Entities;

namespace Business.Web.ViewModels;

public class DashboardViewModel
{
    public int ActiveProjectCount { get; set; }
    public int OpenOrderCount { get; set; }
    public int PendingMaterialRequestCount { get; set; }
    public int SupplierCount { get; set; }
    public int MyOpenTaskCount { get; set; }
    public int MyOverdueTaskCount { get; set; }
    public int MyDueSoonTaskCount { get; set; }
    public string MyTaskBannerTitle { get; set; } = string.Empty;
    public string MyTaskBannerMessage { get; set; } = string.Empty;
    public IReadOnlyList<DashboardUserTaskViewModel> MyTasks { get; set; } = [];
    public IReadOnlyList<Project> RecentProjects { get; set; } = [];
    public IReadOnlyList<PurchaseOrder> RecentOrders { get; set; } = [];
    public IReadOnlyList<MaterialRequest> MaterialRequests { get; set; } = [];
}
