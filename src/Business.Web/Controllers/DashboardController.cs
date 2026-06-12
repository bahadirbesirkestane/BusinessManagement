using Business.Application.Services;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewDashboard)]
public class DashboardController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IMaterialRequestService _materialRequestService;
    private readonly ISupplierService _supplierService;
    private readonly ApplicationDbContext _context;

    public DashboardController(
        IProjectService projectService,
        IPurchaseOrderService purchaseOrderService,
        IMaterialRequestService materialRequestService,
        ISupplierService supplierService,
        ApplicationDbContext context)
    {
        _projectService = projectService;
        _purchaseOrderService = purchaseOrderService;
        _materialRequestService = materialRequestService;
        _supplierService = supplierService;
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new DashboardViewModel
        {
            ActiveProjectCount = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .CountAsync(x => x.Status != ProjectStatus.Completed && x.Status != ProjectStatus.Cancelled, cancellationToken),
            OpenOrderCount = await _context.PurchaseOrders
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .CountAsync(x => x.Status != PurchaseOrderStatus.Delivered && x.Status != PurchaseOrderStatus.Cancelled, cancellationToken),
            PendingMaterialRequestCount = await _context.MaterialRequests
                .AsNoTracking()
                .ApplyProjectRecordVisibility(User)
                .CountAsync(x => x.Status == MaterialRequestStatus.Requested, cancellationToken),
            SupplierCount = await _supplierService.GetCountAsync(cancellationToken),
            RecentProjects = await _context.Projects
                .Include(x => x.Customer)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .OrderByDescending(x => x.CreatedAt)
                .Take(6)
                .ToListAsync(cancellationToken),
            RecentOrders = await _context.PurchaseOrders
                .Include(x => x.Project)
                .Include(x => x.Supplier)
                .Include(x => x.Material)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .OrderByDescending(x => x.CreatedAt)
                .Take(6)
                .ToListAsync(cancellationToken),
            MaterialRequests = await _context.MaterialRequests
                .Include(x => x.Project)
                .Include(x => x.Material)
                .AsNoTracking()
                .ApplyProjectRecordVisibility(User)
                .OrderByDescending(x => x.CreatedAt)
                .Take(6)
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Bugünkü İşler";
        return View("WorkItems", await GetWorkItemsAsync(DateTime.Today, DateTime.Today, includeOverdue: false, cancellationToken));
    }

    public async Task<IActionResult> Overdue(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Geciken İşler";
        return View("WorkItems", await GetWorkItemsAsync(null, DateTime.Today.AddDays(-1), includeOverdue: true, cancellationToken));
    }

    public async Task<IActionResult> CriticalAlerts(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Kritik Uyarılar";
        var today = DateTime.Today;
        var items = new List<DashboardWorkItemViewModel>();

        items.AddRange(await _context.ProjectTasks
            .Include(x => x.Project)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x =>
                x.Priority == ProjectPriority.Critical &&
                x.Status != WorkTaskStatus.Done &&
                x.Status != WorkTaskStatus.Cancelled)
            .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
            .Take(60)
            .Select(x => new DashboardWorkItemViewModel
            {
                Module = "Görev",
                Title = x.Title,
                Subtitle = x.Project == null ? x.ManualProjectName : x.Project.Code,
                StatusText = x.Status.ToDisplayName(),
                StatusCss = x.Status.ToString().ToLower(),
                Date = x.DueDate,
                Controller = "ProjectTasks",
                Id = x.Id
            })
            .ToListAsync(cancellationToken));

        items.AddRange(await _context.PurchaseOrders
            .Include(x => x.Project)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x =>
                x.ExpectedArrivalDate.HasValue &&
                x.ExpectedArrivalDate.Value.Date <= today.AddDays(3) &&
                x.Status != PurchaseOrderStatus.Delivered &&
                x.Status != PurchaseOrderStatus.Cancelled)
            .OrderBy(x => x.ExpectedArrivalDate)
            .Take(60)
            .Select(x => new DashboardWorkItemViewModel
            {
                Module = "Sipariş",
                Title = x.Content,
                Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                StatusText = x.Status.ToDisplayName(),
                StatusCss = x.Status.ToString().ToLower(),
                Date = x.ExpectedArrivalDate,
                Controller = "PurchaseOrders",
                Id = x.Id
            })
            .ToListAsync(cancellationToken));

        items.AddRange(await _context.StockItems
            .Include(x => x.Material)
            .AsNoTracking()
            .Where(x => x.Status == StockStatus.LowStock || x.Status == StockStatus.OutOfStock)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.Name)
            .Take(60)
            .Select(x => new DashboardWorkItemViewModel
            {
                Module = "Stok",
                Title = x.Name,
                Subtitle = x.Material == null ? x.Location : x.Material.Name,
                StatusText = x.Status.ToDisplayName(),
                StatusCss = x.Status.ToString().ToLower(),
                Date = x.UpdatedAt ?? x.CreatedAt,
                Controller = "Stock",
                Action = x.MaterialId.HasValue ? "Details" : "Index",
                Id = x.MaterialId ?? x.Id
            })
            .ToListAsync(cancellationToken));

        return View("WorkItems", items.OrderBy(x => x.Date ?? DateTime.MaxValue).ToList());
    }

    private async Task<IReadOnlyList<DashboardWorkItemViewModel>> GetWorkItemsAsync(DateTime? dateFrom, DateTime dateTo, bool includeOverdue, CancellationToken cancellationToken)
    {
        var items = new List<DashboardWorkItemViewModel>();

        var taskQuery = _context.ProjectTasks
            .Include(x => x.Project)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x =>
                x.DueDate.HasValue &&
                x.Status != WorkTaskStatus.Done &&
                x.Status != WorkTaskStatus.Cancelled);

        taskQuery = includeOverdue
            ? taskQuery.Where(x => x.DueDate!.Value.Date <= dateTo.Date)
            : taskQuery.Where(x => x.DueDate!.Value.Date >= dateFrom!.Value.Date && x.DueDate!.Value.Date <= dateTo.Date);

        items.AddRange(await taskQuery
            .OrderBy(x => x.DueDate)
            .Take(120)
            .Select(x => new DashboardWorkItemViewModel
            {
                Module = "Görev",
                Title = x.Title,
                Subtitle = x.Project == null ? x.ManualProjectName : x.Project.Code,
                StatusText = x.Status.ToDisplayName(),
                StatusCss = x.Status.ToString().ToLower(),
                Date = x.DueDate,
                Controller = "ProjectTasks",
                Id = x.Id
            })
            .ToListAsync(cancellationToken));

        var orderQuery = _context.PurchaseOrders
            .Include(x => x.Project)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x =>
                x.ExpectedArrivalDate.HasValue &&
                x.Status != PurchaseOrderStatus.Delivered &&
                x.Status != PurchaseOrderStatus.Cancelled);

        orderQuery = includeOverdue
            ? orderQuery.Where(x => x.ExpectedArrivalDate!.Value.Date <= dateTo.Date)
            : orderQuery.Where(x => x.ExpectedArrivalDate!.Value.Date >= dateFrom!.Value.Date && x.ExpectedArrivalDate!.Value.Date <= dateTo.Date);

        items.AddRange(await orderQuery
            .OrderBy(x => x.ExpectedArrivalDate)
            .Take(120)
            .Select(x => new DashboardWorkItemViewModel
            {
                Module = "Sipariş",
                Title = x.Content,
                Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                StatusText = x.Status.ToDisplayName(),
                StatusCss = x.Status.ToString().ToLower(),
                Date = x.ExpectedArrivalDate,
                Controller = "PurchaseOrders",
                Id = x.Id
            })
            .ToListAsync(cancellationToken));

        var materialQuery = _context.MaterialRequests
            .Include(x => x.Project)
            .AsNoTracking()
            .ApplyProjectRecordVisibility(User)
            .Where(x =>
                x.Status != MaterialRequestStatus.Fulfilled &&
                x.Status != MaterialRequestStatus.Cancelled);

        materialQuery = includeOverdue
            ? materialQuery.Where(x => x.NeededBy.Date <= dateTo.Date)
            : materialQuery.Where(x => x.NeededBy.Date >= dateFrom!.Value.Date && x.NeededBy.Date <= dateTo.Date);

        items.AddRange(await materialQuery
            .OrderBy(x => x.NeededBy)
            .Take(120)
            .Select(x => new DashboardWorkItemViewModel
            {
                Module = "Malzeme ihtiyacı",
                Title = x.RequestedItem,
                Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                StatusText = x.Status.ToDisplayName(),
                StatusCss = x.Status.ToString().ToLower(),
                Date = x.NeededBy,
                Controller = "MaterialRequests",
                Id = x.Id
            })
            .ToListAsync(cancellationToken));

        return items.OrderBy(x => x.Date ?? DateTime.MaxValue).ToList();
    }
}
