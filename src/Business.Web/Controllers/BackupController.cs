using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class BackupController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public BackupController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Modules = GetModules();
        ViewBag.Counts = await GetCountsAsync(cancellationToken);
        return View();
    }

    public async Task<IActionResult> DownloadAll(CancellationToken cancellationToken)
    {
        var sheets = new List<ExcelSheet>();
        foreach (var module in GetModules())
        {
            sheets.AddRange(await BuildSheetsAsync(module.Key, cancellationToken));
        }

        return ExcelFile(sheets, $"is-yonetimi-yedek-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> Download(string module, CancellationToken cancellationToken)
    {
        var selectedModule = GetModules().FirstOrDefault(x => x.Key.Equals(module, StringComparison.OrdinalIgnoreCase));
        if (selectedModule is null)
        {
            return NotFound();
        }

        var sheets = await BuildSheetsAsync(selectedModule.Key, cancellationToken);
        return ExcelFile(sheets, $"{selectedModule.Key.ToLowerInvariant()}-yedek-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    private FileContentResult ExcelFile(IReadOnlyList<ExcelSheet> sheets, string fileName)
    {
        var bytes = ExcelWorkbookBuilder.Build(sheets);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static IReadOnlyList<BackupModuleViewModel> GetModules()
    {
        return
        [
            new("Projects", "Projeler", "Proje kartları ve müşteri bağlantıları"),
            new("ProjectTasks", "Görevler", "Görev, sorumlu ve atama bilgileri"),
            new("PurchaseOrders", "Siparişler", "Genel ve proje siparişleri"),
            new("Materials", "Malzemeler", "Malzeme kartları ve kategoriler"),
            new("MaterialRequests", "Malzeme İhtiyaçları", "Proje ve genel ihtiyaç kayıtları"),
            new("Stock", "Stok", "Stok satırları, ölçü ve durum bilgileri"),
            new("Customers", "Müşteriler", "Müşteri kartları"),
            new("Suppliers", "Tedarikçiler", "Tedarikçi kartları"),
            new("Invoices", "Faturalar", "Fatura başlıkları ve kalemleri"),
            new("Costs", "Maliyetler", "Proje, sipariş, malzeme ve genel giderler"),
            new("Users", "Kullanıcılar", "Kullanıcı, rol ve departman bilgileri"),
            new("Activities", "Yorumlar ve Dosyalar", "Kayıtlara eklenen yorum ve dosya metaverileri")
        ];
    }

    private async Task<Dictionary<string, int>> GetCountsAsync(CancellationToken cancellationToken)
    {
        return new Dictionary<string, int>
        {
            ["Projects"] = await _context.Projects.CountAsync(cancellationToken),
            ["ProjectTasks"] = await _context.ProjectTasks.CountAsync(cancellationToken),
            ["PurchaseOrders"] = await _context.PurchaseOrders.CountAsync(cancellationToken),
            ["Materials"] = await _context.Materials.CountAsync(cancellationToken),
            ["MaterialRequests"] = await _context.MaterialRequests.CountAsync(cancellationToken),
            ["Stock"] = await _context.StockItems.CountAsync(cancellationToken),
            ["Customers"] = await _context.Customers.CountAsync(cancellationToken),
            ["Suppliers"] = await _context.Suppliers.CountAsync(cancellationToken),
            ["Invoices"] = await _context.Invoices.CountAsync(cancellationToken),
            ["Costs"] = await _context.ProjectCostItems.CountAsync(cancellationToken),
            ["Users"] = await _userManager.Users.CountAsync(cancellationToken),
            ["Activities"] = await _context.RecordComments.CountAsync(cancellationToken) + await _context.RecordFiles.CountAsync(cancellationToken)
        };
    }

    private async Task<IReadOnlyList<ExcelSheet>> BuildSheetsAsync(string module, CancellationToken cancellationToken)
    {
        return module switch
        {
            "Projects" => [await ProjectsSheet(cancellationToken)],
            "ProjectTasks" => [await TasksSheet(cancellationToken)],
            "PurchaseOrders" => [await PurchaseOrdersSheet(cancellationToken)],
            "Materials" => [await MaterialsSheet(cancellationToken), await MaterialCategoriesSheet(cancellationToken)],
            "MaterialRequests" => [await MaterialRequestsSheet(cancellationToken)],
            "Stock" => [await StockSheet(cancellationToken)],
            "Customers" => [await CustomersSheet(cancellationToken)],
            "Suppliers" => [await SuppliersSheet(cancellationToken)],
            "Invoices" => [await InvoicesSheet(cancellationToken), await InvoiceLinesSheet(cancellationToken)],
            "Costs" => [await CostsSheet(cancellationToken)],
            "Users" => [await UsersSheet(cancellationToken), await RolesSheet(cancellationToken), await DepartmentsSheet(cancellationToken)],
            "Activities" => [await CommentsSheet(cancellationToken), await FilesSheet(cancellationToken)],
            _ => []
        };
    }

    private async Task<ExcelSheet> ProjectsSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.Projects
            .Include(x => x.Customer)
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new object?[]
            {
                x.Code, x.Name, x.Customer != null ? x.Customer.Name : x.CustomerName, x.Status.ToDisplayName(), x.Priority.ToDisplayName(),
                x.StartDate, x.TargetEndDate, x.CompletedAt, x.Budget, x.Currency, x.Notes, x.CreatedAt, x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Projeler", ["Kod", "Proje", "Müşteri", "Durum", "Öncelik", "Başlangıç", "Hedef Bitiş", "Tamamlanma", "Bütçe", "Para Birimi", "Not", "Oluşturma", "Güncelleme"], rows);
    }

    private async Task<ExcelSheet> TasksSheet(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.Id, cancellationToken);
        var assignments = await _context.ProjectTaskAssignments
            .AsNoTracking()
            .GroupBy(x => x.ProjectTaskId)
            .Select(x => new { TaskId = x.Key, UserIds = x.Select(y => y.UserId).ToList() })
            .ToListAsync(cancellationToken);
        var assignmentMap = assignments.ToDictionary(x => x.TaskId, x => string.Join(", ", x.UserIds.Select(id => users.GetValueOrDefault(id, id))));

        var tasks = await _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Customer)
            .Include(x => x.TaskCategory)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var rows = tasks.Select(x => (IReadOnlyList<object?>)
        [
            x.Title, x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : x.ManualProjectName,
            x.Customer != null ? x.Customer.Name : x.ManualCustomerName, x.TaskCategory?.Name, x.Status.ToDisplayName(),
            x.Priority.ToDisplayName(), x.ProgressPercent, users.GetValueOrDefault(x.ResponsibleUserId ?? string.Empty, string.Empty),
            users.GetValueOrDefault(x.AssignedToUserId ?? string.Empty, string.Empty), assignmentMap.GetValueOrDefault(x.Id, string.Empty),
            x.StartDate, x.DueDate, x.CompletedAt, x.Description, x.CreatedAt, x.UpdatedAt
        ]).ToList();

        return new ExcelSheet("Görevler", ["Başlık", "Proje", "Müşteri", "Kategori", "Durum", "Öncelik", "İlerleme", "Sorumlu", "Ana Atanan", "Atananlar", "Başlangıç", "Termin", "Tamamlanma", "Açıklama", "Oluşturma", "Güncelleme"], rows);
    }

    private async Task<ExcelSheet> PurchaseOrdersSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .Select(x => new object?[]
            {
                x.OrderNumber, x.Scope.ToDisplayName(), x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : null,
                x.Supplier != null ? x.Supplier.Name : null, x.Material != null ? x.Material.Name : null, x.Content,
                x.Quantity, x.QuantityText, x.Unit, x.Quality, x.Status.ToDisplayName(), x.OrderDate, x.ExpectedArrivalDate,
                x.ArrivalDate, x.RequestedBy, x.PaymentTerm, x.UnitPrice, x.UnitPriceText, x.OrderTotal, x.Currency, x.Notes, x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Siparişler", ["No", "Kapsam", "Proje", "Tedarikçi", "Malzeme", "İçerik", "Miktar", "Miktar Metni", "Birim", "Kalite", "Durum", "Sipariş Tarihi", "Beklenen Varış", "Geliş", "Sipariş Veren", "Vade", "Birim Fiyat", "Birim Fiyat Metni", "Tutar", "Para Birimi", "Not", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> MaterialsSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.Materials
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new object?[] { x.Name, x.CategoryName, x.Category.ToDisplayName(), x.Type, x.Grade, x.Surface, x.Dimensions, x.Unit, x.Description, x.Notes, x.CreatedAt })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Malzemeler", ["Ad", "Kategori", "Kategori Enum", "Tür", "Kalite", "Yüzey", "Ölçü", "Birim", "Açıklama", "Not", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> MaterialCategoriesSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.MaterialCategoryDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new object?[] { x.Name, x.Description, x.IsActive, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Malzeme Kategorileri", ["Ad", "Açıklama", "Aktif", "Oluşturma", "Güncelleme"], rows);
    }

    private async Task<ExcelSheet> MaterialRequestsSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.MaterialRequests
            .Include(x => x.Project)
            .Include(x => x.Material)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new object?[]
            {
                x.RequestedItem, x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : null, x.Material != null ? x.Material.Name : null,
                x.Quantity, x.QuantityText, x.Unit, x.Quality, x.Status.ToDisplayName(), x.NeededBy, x.Notes, x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Malzeme İhtiyaçları", ["İhtiyaç", "Proje", "Malzeme", "Miktar", "Miktar Metni", "Birim", "Kalite", "Durum", "İhtiyaç Tarihi", "Not", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> StockSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.StockItems
            .Include(x => x.Material)
            .AsNoTracking()
            .OrderBy(x => x.Material!.Name)
            .ThenBy(x => x.Name)
            .Select(x => new object?[] { x.Material != null ? x.Material.Name : null, x.Name, x.Thickness, x.Dimensions, x.Quantity, x.QuantityText, x.Unit, x.Status.ToDisplayName(), x.Location, x.Notes, x.CreatedAt })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Stok", ["Malzeme", "Tür", "Kalınlık", "Ölçü", "Miktar", "Miktar Metni", "Birim", "Durum", "Konum", "Not", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> CustomersSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.Customers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new object?[] { x.Name, x.TaxNumber, x.TaxOffice, x.ContactPerson, x.Email, x.Phone, x.Website, x.PaymentTerm, x.Address, x.Notes, x.IsActive, x.CreatedAt })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Müşteriler", ["Ad", "Vergi No", "Vergi Dairesi", "Yetkili", "E-posta", "Telefon", "Web", "Vade", "Adres", "Not", "Aktif", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> SuppliersSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new object?[] { x.Name, x.Type, x.Email, x.Phone, x.PaymentTerm, x.Address, x.Website, x.Reliability.ToDisplayName(), x.Notes, x.CreatedAt })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Tedarikçiler", ["Ad", "Tür", "E-posta", "Telefon", "Vade", "Adres", "Web", "Güvenilirlik", "Not", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> InvoicesSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.Invoices
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .Include(x => x.Project)
            .Include(x => x.PurchaseOrder)
            .AsNoTracking()
            .OrderByDescending(x => x.IssueDate)
            .Select(x => new object?[]
            {
                x.InvoiceNumber, x.Type.ToDisplayName(), x.Status.ToDisplayName(), x.Customer != null ? x.Customer.Name : null,
                x.Supplier != null ? x.Supplier.Name : null, x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : null,
                x.PurchaseOrder != null ? x.PurchaseOrder.OrderNumber : null, x.IssueDate, x.DueDate, x.PaidAt,
                x.SubTotal, x.VatTotal, x.DiscountTotal, x.GrandTotal, x.Currency, x.PaymentTerm, x.Notes, x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Faturalar", ["No", "Tip", "Durum", "Müşteri", "Tedarikçi", "Proje", "Sipariş", "Tarih", "Vade Tarihi", "Ödeme", "Ara Toplam", "KDV", "İskonto", "Genel Toplam", "Para Birimi", "Vade", "Not", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> InvoiceLinesSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.InvoiceLines
            .Include(x => x.Invoice)
            .Include(x => x.Material)
            .AsNoTracking()
            .OrderByDescending(x => x.Invoice.IssueDate)
            .Select(x => new object?[] { x.Invoice.InvoiceNumber, x.Material != null ? x.Material.Name : null, x.Description, x.Quantity, x.Unit, x.UnitPrice, x.VatRate, x.DiscountAmount, x.LineTotal, x.Notes })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Fatura Kalemleri", ["Fatura No", "Malzeme", "Açıklama", "Miktar", "Birim", "Birim Fiyat", "KDV", "İskonto", "Satır Toplamı", "Not"], rows);
    }

    private async Task<ExcelSheet> CostsSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.ProjectCostItems
            .Include(x => x.Project)
            .Include(x => x.PurchaseOrder)
            .AsNoTracking()
            .OrderByDescending(x => x.CostDate)
            .Select(x => new object?[]
            {
                x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : null, x.PurchaseOrder != null ? x.PurchaseOrder.OrderNumber : null,
                x.Type.ToDisplayName(), x.Description, x.Amount, x.Currency, x.CostDate, x.Notes, x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Maliyetler", ["Proje", "Sipariş", "Tip", "Açıklama", "Tutar", "Para Birimi", "Tarih", "Not", "Oluşturma"], rows);
    }

    private async Task<ExcelSheet> UsersSheet(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var departments = await _context.Departments.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var rows = new List<IReadOnlyList<object?>>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add([
                user.FullName, user.Email, user.PhoneNumber, user.DepartmentId.HasValue ? departments.GetValueOrDefault(user.DepartmentId.Value) : null,
                string.Join(", ", roles), user.IsActive, user.EmailConfirmed, user.LockoutEnd
            ]);
        }

        return new ExcelSheet("Kullanıcılar", ["Ad Soyad", "E-posta", "Telefon", "Departman", "Roller", "Aktif", "E-posta Onaylı", "Kilit Bitişi"], rows);
    }

    private async Task<ExcelSheet> RolesSheet(CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var rows = new List<IReadOnlyList<object?>>();
        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            rows.Add([role.Name, string.Join(", ", claims.Where(x => x.Type == AppClaimTypes.Permission).Select(x => x.Value))]);
        }

        return new ExcelSheet("Roller", ["Rol", "Yetkiler"], rows);
    }

    private async Task<ExcelSheet> DepartmentsSheet(CancellationToken cancellationToken)
    {
        var rows = await _context.Departments
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new object?[] { x.Name, x.Description, x.IsActive, x.CreatedAt, x.UpdatedAt })
            .ToListAsync(cancellationToken);

        return new ExcelSheet("Departmanlar", ["Ad", "Açıklama", "Aktif", "Oluşturma", "Güncelleme"], rows);
    }

    private async Task<ExcelSheet> CommentsSheet(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.Id, cancellationToken);
        var comments = await _context.RecordComments.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var rows = comments.Select(x => (IReadOnlyList<object?>)[
            x.OwnerType.ToDisplayName(), x.OwnerId, x.CommentText, users.GetValueOrDefault(x.CreatedByUserId ?? string.Empty, string.Empty), x.CreatedAt
        ]).ToList();

        return new ExcelSheet("Yorumlar", ["Kayıt Tipi", "Kayıt Id", "Yorum", "Ekleyen", "Tarih"], rows);
    }

    private async Task<ExcelSheet> FilesSheet(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.Email ?? x.Id, cancellationToken);
        var files = await _context.RecordFiles.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var rows = files.Select(x => (IReadOnlyList<object?>)[
            x.OwnerType.ToDisplayName(), x.OwnerId, x.OriginalFileName, x.StoredFileName, x.ContentType, x.Size, x.Description, users.GetValueOrDefault(x.CreatedByUserId ?? string.Empty, string.Empty), x.CreatedAt
        ]).ToList();

        return new ExcelSheet("Dosyalar", ["Kayıt Tipi", "Kayıt Id", "Orijinal Dosya", "Saklanan Dosya", "İçerik Tipi", "Boyut", "Açıklama", "Ekleyen", "Tarih"], rows);
    }
}

public sealed record BackupModuleViewModel(string Key, string Title, string Description);
