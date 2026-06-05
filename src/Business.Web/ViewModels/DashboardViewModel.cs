using Business.Domain.Entities;

namespace Business.Web.ViewModels;

public class DashboardViewModel
{
    public int ActiveProjectCount { get; set; }
    public int OpenOrderCount { get; set; }
    public int PendingMaterialRequestCount { get; set; }
    public int SupplierCount { get; set; }
    public IReadOnlyList<Project> RecentProjects { get; set; } = [];
    public IReadOnlyList<PurchaseOrder> RecentOrders { get; set; } = [];
    public IReadOnlyList<MaterialRequest> MaterialRequests { get; set; } = [];
}
