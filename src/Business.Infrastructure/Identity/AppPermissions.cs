namespace Business.Infrastructure.Identity;

public static class AppPermissions
{
    public const string DashboardView = "dashboard.view";
    public const string ProjectsView = "projects.view";
    public const string ProjectsCreate = "projects.create";
    public const string ProjectsUpdate = "projects.update";
    public const string ProjectsDelete = "projects.delete";
    public const string ProjectsChangeStatus = "projects.change-status";
    public const string ProjectsManage = "projects.manage";
    public const string TasksView = "tasks.view";
    public const string TasksViewAll = "tasks.view-all";
    public const string TasksCreate = "tasks.create";
    public const string TasksUpdate = "tasks.update";
    public const string TasksDelete = "tasks.delete";
    public const string TasksChangeStatus = "tasks.change-status";
    public const string TasksComplete = "tasks.complete";
    public const string TasksManage = "tasks.manage";
    public const string PurchasingView = "purchasing.view";
    public const string PurchasingCreate = "purchasing.create";
    public const string PurchasingUpdate = "purchasing.update";
    public const string PurchasingDelete = "purchasing.delete";
    public const string PurchasingChangeStatus = "purchasing.change-status";
    public const string PurchasingManage = "purchasing.manage";
    public const string SuppliersView = "suppliers.view";
    public const string SuppliersManage = "suppliers.manage";
    public const string MaterialsView = "materials.view";
    public const string MaterialsManage = "materials.manage";
    public const string StockView = "stock.view";
    public const string StockManage = "stock.manage";
    public const string ProductionUpdatesView = "production-updates.view";
    public const string MaterialRequestsView = "material-requests.view";
    public const string MaterialRequestsManage = "material-requests.manage";
    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";
    public const string InvoicesView = "invoices.view";
    public const string InvoicesManage = "invoices.manage";
    public const string CostsView = "costs.view";
    public const string CostsManage = "costs.manage";
    public const string ProjectBudgetView = "project-budget.view";
    public const string ProjectBudgetManage = "project-budget.manage";
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string SettingsManage = "settings.manage";
    public const string CompanyFilesView = "companyfiles.view";
    public const string CompanyFilesManage = "companyfiles.manage";

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(DashboardView, "Dashboard", "Dashboard görüntüle"),
        new(ProjectsView, "Projeler", "Projeleri görüntüle"),
        new(ProjectsCreate, "Projeler", "Proje oluştur"),
        new(ProjectsUpdate, "Projeler", "Proje güncelle"),
        new(ProjectsDelete, "Projeler", "Proje sil"),
        new(ProjectsChangeStatus, "Projeler", "Proje durumu değiştir"),
        new(ProjectsManage, "Projeler", "Proje tam yönetim"),
        new(TasksView, "Görevler", "Görevleri görüntüle"),
        new(TasksViewAll, "Görevler", "Tüm görevleri görüntüle"),
        new(TasksCreate, "Görevler", "Görev oluştur"),
        new(TasksUpdate, "Görevler", "Görev güncelle"),
        new(TasksDelete, "Görevler", "Görev sil"),
        new(TasksChangeStatus, "Görevler", "Görev durumu değiştir"),
        new(TasksComplete, "Görevler", "Görevi tamamlandı yap"),
        new(TasksManage, "Görevler", "Görev tam yönetim"),
        new(PurchasingView, "Siparişler", "Siparişleri görüntüle"),
        new(PurchasingCreate, "Siparişler", "Sipariş oluştur"),
        new(PurchasingUpdate, "Siparişler", "Sipariş güncelle"),
        new(PurchasingDelete, "Siparişler", "Sipariş sil"),
        new(PurchasingChangeStatus, "Siparişler", "Sipariş durumu değiştir"),
        new(PurchasingManage, "Siparişler", "Sipariş tam yönetim"),
        new(SuppliersView, "Tedarikçiler", "Tedarikçileri görüntüle"),
        new(SuppliersManage, "Tedarikçiler", "Tedarikçi yönet"),
        new(MaterialsView, "Malzemeler", "Malzemeleri görüntüle"),
        new(MaterialsManage, "Malzemeler", "Malzeme yönet"),
        new(StockView, "Stok / Depo", "Stok görüntüle"),
        new(StockManage, "Stok / Depo", "Stok yönet"),
        new(ProductionUpdatesView, "Üretim", "Üretim güncellemelerini görüntüle"),
        new(MaterialRequestsView, "İhtiyaç", "İhtiyaç listesini görüntüle"),
        new(MaterialRequestsManage, "İhtiyaç", "İhtiyaç listesi yönet"),
        new(CustomersView, "Müşteriler", "Müşterileri görüntüle"),
        new(CustomersManage, "Müşteriler", "Müşteri yönet"),
        new(InvoicesView, "Faturalar", "Faturaları görüntüle"),
        new(InvoicesManage, "Faturalar", "Fatura yönet"),
        new(CostsView, "Maliyet", "Maliyetleri görüntüle"),
        new(CostsManage, "Maliyet", "Maliyetleri yönet"),
        new(ProjectBudgetView, "Maliyet", "Proje bütçesini görüntüle"),
        new(ProjectBudgetManage, "Maliyet", "Proje bütçesi ve kur yönet"),
        new(UsersView, "Kullanıcılar ve Yetkiler", "Kullanıcıları görüntüle"),
        new(UsersManage, "Kullanıcılar ve Yetkiler", "Kullanıcı yönet"),
        new(RolesManage, "Kullanıcılar ve Yetkiler", "Rol ve izin yönet"),
        new(SettingsManage, "Ayarlar", "Sistem ayarlarını yönet"),
        new(CompanyFilesView, "Dosya Yönetimi", "Firma dosyalarını görüntüle"),
        new(CompanyFilesManage, "Dosya Yönetimi", "Firma dosyalarını yönet")
    ];
}

public record PermissionDefinition(string Value, string Group, string Name);
