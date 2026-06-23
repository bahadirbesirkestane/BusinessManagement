using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IProjectService projectService,
        IPurchaseOrderService purchaseOrderService,
        IMaterialRequestService materialRequestService,
        ISupplierService supplierService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _projectService = projectService;
        _purchaseOrderService = purchaseOrderService;
        _materialRequestService = materialRequestService;
        _supplierService = supplierService;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var userId = _userManager.GetUserId(User);
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

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var myOpenTaskQuery = BuildUserTaskQuery(userId)
                .Where(x => x.Status != WorkTaskStatus.Done && x.Status != WorkTaskStatus.Cancelled);

            model.MyOpenTaskCount = await myOpenTaskQuery.CountAsync(cancellationToken);
            model.MyOverdueTaskCount = await myOpenTaskQuery.CountAsync(x => x.DueDate.HasValue && x.DueDate.Value.Date < today, cancellationToken);
            model.MyDueSoonTaskCount = await myOpenTaskQuery.CountAsync(x => x.DueDate.HasValue && x.DueDate.Value.Date >= today && x.DueDate.Value.Date <= today.AddDays(3), cancellationToken);
            model.MyTasks = await MapUserTasksAsync(
                myOpenTaskQuery
                    .OrderByDescending(x => x.Priority)
                    .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
                    .ThenByDescending(x => x.CreatedAt)
                    .Take(5),
                userId,
                today,
                cancellationToken);

            if (model.MyOpenTaskCount > 0)
            {
                model.MyTaskBannerTitle = "Açık görevleriniz var";
                model.MyTaskBannerMessage = model.MyOverdueTaskCount > 0
                    ? $"{model.MyOverdueTaskCount} görevinizin tarihi geçmiş görünüyor. Öncelikli işleri aşağıdan hızlıca açabilirsiniz."
                    : "Sorumlu olduğunuz ve size atanan işler";
            }
            else
            {
                model.MyTaskBannerTitle = "Şu an açık göreviniz görünmüyor";
                model.MyTaskBannerMessage = "Yeni atamaları ve yaklaşan işleri takip etmek için İşlerim ekranını kullanabilirsiniz.";
            }
        }

        return View(model);
    }

    public async Task<IActionResult> Workspace(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "İşlerim";

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var today = DateTime.Today;
        var openTaskQuery = BuildUserTaskQuery(userId)
            .Where(x => x.Status != WorkTaskStatus.Done && x.Status != WorkTaskStatus.Cancelled);

        var model = new DashboardWorkspaceViewModel
        {
            OpenTaskCount = await openTaskQuery.CountAsync(cancellationToken),
            OverdueTaskCount = await openTaskQuery.CountAsync(x => x.DueDate.HasValue && x.DueDate.Value.Date < today, cancellationToken),
            DueTodayCount = await openTaskQuery.CountAsync(x => x.DueDate.HasValue && x.DueDate.Value.Date == today, cancellationToken),
            WaitingReviewCount = await openTaskQuery.CountAsync(x => x.Status == WorkTaskStatus.InReview, cancellationToken),
            Tasks = await MapUserTasksAsync(
                openTaskQuery
                    .OrderByDescending(x => x.Priority == ProjectPriority.Critical)
                    .ThenByDescending(x => x.Priority == ProjectPriority.High)
                    .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
                    .ThenByDescending(x => x.CreatedAt)
                    .Take(8),
                userId,
                today,
                cancellationToken)
        };

        return View(model);
    }

    public async Task<IActionResult> Today(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Bugünkü İşler";
        ViewData["InfoText"] = "Bugün aktif tarih aralığında olan açık görevler ile bugün sürecinde olan açık sipariş ve ihtiyaç kayıtları listelenir.";
        return View("WorkItems", await GetTodayWorkItemsAsync(DateTime.Today, cancellationToken));
    }

    public async Task<IActionResult> Overdue(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Geciken İşler";
        ViewData["InfoText"] = "Termin veya ihtiyaç tarihi bugünden önce kalan, henüz tamamlanmamış ya da teslim edilmemiş kayıtlar gösterilir.";
        return View("WorkItems", await GetOverdueWorkItemsAsync(DateTime.Today, cancellationToken));
    }

    public async Task<IActionResult> CriticalAlerts(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Kritik Uyarılar";
        ViewData["InfoText"] = "Kritik öncelikli açık görevler, çok yaklaşan açık siparişler ve kritik stok uyarıları listelenir.";
        var today = DateTime.Today;
        var soonDate = today.AddDays(3).Date;
        var items = new List<DashboardWorkItemViewModel>();

        if (CanViewTasksModule())
        {
            items.AddRange(await _context.ProjectTasks
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x =>
                    x.Priority == ProjectPriority.Critical &&
                    x.Status != WorkTaskStatus.Done &&
                    x.Status != WorkTaskStatus.Cancelled &&
                    (
                        (!x.StartDate.HasValue && !x.DueDate.HasValue) ||
                        (x.StartDate.HasValue && !x.DueDate.HasValue && x.StartDate.Value.Date == today.Date) ||
                        (!x.StartDate.HasValue && x.DueDate.HasValue && x.DueDate.Value.Date <= soonDate) ||
                        (x.StartDate.HasValue && x.DueDate.HasValue &&
                            (
                                (x.StartDate.Value.Date <= today.Date && x.DueDate.Value.Date >= today.Date) ||
                                x.DueDate.Value.Date <= soonDate
                            ))
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .Take(60)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Görev",
                    Title = x.Title,
                    Subtitle = x.Project == null ? x.ManualProjectName : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.DueDate ?? x.StartDate,
                    CreatedAt = x.CreatedAt,
                    Controller = "ProjectTasks",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        if (CanViewPurchasingModule())
        {
            items.AddRange(await _context.PurchaseOrders
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x =>
                    x.Status != PurchaseOrderStatus.Delivered &&
                    x.Status != PurchaseOrderStatus.Cancelled &&
                    (
                        (x.OrderDate.HasValue && x.ExpectedArrivalDate.HasValue &&
                            (
                                (x.OrderDate.Value.Date <= today.Date && x.ExpectedArrivalDate.Value.Date >= today.Date) ||
                                x.ExpectedArrivalDate.Value.Date <= soonDate
                            )) ||
                        (!x.OrderDate.HasValue && x.ExpectedArrivalDate.HasValue && x.ExpectedArrivalDate.Value.Date <= soonDate) ||
                        (x.OrderDate.HasValue && !x.ExpectedArrivalDate.HasValue && x.OrderDate.Value.Date == today.Date)
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .Take(60)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Sipariş",
                    Title = x.Content,
                    Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.ExpectedArrivalDate ?? x.OrderDate,
                    CreatedAt = x.CreatedAt,
                    Controller = "PurchaseOrders",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        if (CanViewStockModule())
        {
            items.AddRange(await _context.StockItems
                .Include(x => x.Material)
                .AsNoTracking()
                .Where(x => x.Status == StockStatus.LowStock || x.Status == StockStatus.OutOfStock)
                .OrderByDescending(x => x.CreatedAt)
                .Take(60)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Stok",
                    Title = x.Name,
                    Subtitle = x.Material == null ? x.Location : x.Material.Name,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.UpdatedAt ?? x.CreatedAt,
                    CreatedAt = x.CreatedAt,
                    Controller = "Stock",
                    Action = x.MaterialId.HasValue ? "Details" : "Index",
                    Id = x.MaterialId ?? x.Id
                })
                .ToListAsync(cancellationToken));
        }

        return View("WorkItems", items
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Date ?? DateTime.MaxValue)
            .ToList());
    }

    private async Task<IReadOnlyList<DashboardWorkItemViewModel>> GetTodayWorkItemsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var items = new List<DashboardWorkItemViewModel>();

        if (CanViewTasksModule())
        {
            items.AddRange(await _context.ProjectTasks
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x =>
                    x.Status != WorkTaskStatus.Done &&
                    x.Status != WorkTaskStatus.Cancelled &&
                    (
                        (x.StartDate.HasValue && x.DueDate.HasValue && x.StartDate.Value.Date <= today.Date && x.DueDate.Value.Date >= today.Date) ||
                        (x.StartDate.HasValue && !x.DueDate.HasValue && x.StartDate.Value.Date == today.Date) ||
                        (!x.StartDate.HasValue && x.DueDate.HasValue && x.DueDate.Value.Date == today.Date)
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .Take(120)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Görev",
                    Title = x.Title,
                    Subtitle = x.Project == null ? x.ManualProjectName : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.DueDate ?? x.StartDate,
                    CreatedAt = x.CreatedAt,
                    Controller = "ProjectTasks",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        if (CanViewPurchasingModule())
        {
            items.AddRange(await _context.PurchaseOrders
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x =>
                    x.Status != PurchaseOrderStatus.Delivered &&
                    x.Status != PurchaseOrderStatus.Cancelled &&
                    (
                        (x.OrderDate.HasValue && x.ExpectedArrivalDate.HasValue && x.OrderDate.Value.Date <= today.Date && x.ExpectedArrivalDate.Value.Date >= today.Date) ||
                        (x.OrderDate.HasValue && !x.ExpectedArrivalDate.HasValue && x.OrderDate.Value.Date == today.Date) ||
                        (!x.OrderDate.HasValue && x.ExpectedArrivalDate.HasValue && x.ExpectedArrivalDate.Value.Date == today.Date)
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .Take(120)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Sipariş",
                    Title = x.Content,
                    Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.ExpectedArrivalDate ?? x.OrderDate,
                    CreatedAt = x.CreatedAt,
                    Controller = "PurchaseOrders",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        if (CanViewMaterialRequestsModule())
        {
            items.AddRange(await _context.MaterialRequests
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyProjectRecordVisibility(User)
                .Where(x =>
                    x.Status != MaterialRequestStatus.Fulfilled &&
                    x.Status != MaterialRequestStatus.Cancelled &&
                    x.NeededBy.Date == today.Date)
                .OrderByDescending(x => x.CreatedAt)
                .Take(120)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Malzeme ihtiyacı",
                    Title = x.RequestedItem,
                    Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.NeededBy,
                    CreatedAt = x.CreatedAt,
                    Controller = "MaterialRequests",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        return items
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Date ?? DateTime.MaxValue)
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardWorkItemViewModel>> GetOverdueWorkItemsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var items = new List<DashboardWorkItemViewModel>();

        if (CanViewTasksModule())
        {
            items.AddRange(await _context.ProjectTasks
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x =>
                    x.Status != WorkTaskStatus.Done &&
                    x.Status != WorkTaskStatus.Cancelled &&
                    x.DueDate.HasValue &&
                    x.DueDate.Value.Date < today.Date)
                .OrderByDescending(x => x.CreatedAt)
                .Take(120)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Görev",
                    Title = x.Title,
                    Subtitle = x.Project == null ? x.ManualProjectName : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.DueDate,
                    CreatedAt = x.CreatedAt,
                    Controller = "ProjectTasks",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        if (CanViewPurchasingModule())
        {
            items.AddRange(await _context.PurchaseOrders
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .Where(x =>
                    x.Status != PurchaseOrderStatus.Delivered &&
                    x.Status != PurchaseOrderStatus.Cancelled &&
                    x.ExpectedArrivalDate.HasValue &&
                    x.ExpectedArrivalDate.Value.Date < today.Date)
                .OrderByDescending(x => x.CreatedAt)
                .Take(120)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Sipariş",
                    Title = x.Content,
                    Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.ExpectedArrivalDate,
                    CreatedAt = x.CreatedAt,
                    Controller = "PurchaseOrders",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        if (CanViewMaterialRequestsModule())
        {
            items.AddRange(await _context.MaterialRequests
                .Include(x => x.Project)
                .AsNoTracking()
                .ApplyProjectRecordVisibility(User)
                .Where(x =>
                    x.Status != MaterialRequestStatus.Fulfilled &&
                    x.Status != MaterialRequestStatus.Cancelled &&
                    x.NeededBy.Date < today.Date)
                .OrderByDescending(x => x.CreatedAt)
                .Take(120)
                .Select(x => new DashboardWorkItemViewModel
                {
                    Module = "Malzeme ihtiyacı",
                    Title = x.RequestedItem,
                    Subtitle = x.Project == null ? "Genel" : x.Project.Code,
                    StatusText = x.Status.ToDisplayName(),
                    StatusCss = x.Status.ToString().ToLowerInvariant(),
                    Date = x.NeededBy,
                    CreatedAt = x.CreatedAt,
                    Controller = "MaterialRequests",
                    Id = x.Id
                })
                .ToListAsync(cancellationToken));
        }

        return items
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Date ?? DateTime.MaxValue)
            .ToList();
    }

    private IQueryable<ProjectTask> BuildUserTaskQuery(string userId)
    {
        return _context.ProjectTasks
            .Include(x => x.Project)
            .Include(x => x.Customer)
            .Include(x => x.Assignments)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x => !x.IsArchived &&
                (x.ResponsibleUserId == userId ||
                 x.AssignedToUserId == userId ||
                 x.Assignments.Any(assignment => assignment.UserId == userId)));
    }

    private async Task<IReadOnlyList<DashboardUserTaskViewModel>> MapUserTasksAsync(
        IQueryable<ProjectTask> query,
        string userId,
        DateTime today,
        CancellationToken cancellationToken)
    {
        return await query
            .Select(x => new DashboardUserTaskViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Context = x.Project != null
                    ? $"{x.Project.Code} - {x.Project.Name}"
                    : x.Customer != null
                        ? x.Customer.Name
                        : x.ManualProjectName ?? x.ManualCustomerName ?? "Genel",
                RoleText = x.ResponsibleUserId == userId && (x.AssignedToUserId == userId || x.Assignments.Any(assignment => assignment.UserId == userId))
                    ? "Sorumlu ve atanan"
                    : x.ResponsibleUserId == userId
                        ? "Sorumlu"
                        : "Atanan",
                StatusText = x.Status.ToDisplayName(),
                StatusCss = x.Status.ToString().ToLowerInvariant(),
                PriorityText = x.Priority.ToDisplayName(),
                PriorityCss = x.Priority.ToString().ToLowerInvariant(),
                DueDate = x.DueDate,
                ProgressPercent = x.ProgressPercent,
                IsOverdue = x.DueDate.HasValue && x.DueDate.Value.Date < today
            })
            .ToListAsync(cancellationToken);
    }

    private bool CanViewTasksModule()
    {
        return HasAnyPermission(AppPermissions.TasksView, AppPermissions.TasksManage, AppPermissions.ProjectsManage, AppPermissions.TasksViewAll);
    }

    private bool CanViewPurchasingModule()
    {
        return HasAnyPermission(AppPermissions.PurchasingView, AppPermissions.PurchasingManage);
    }

    private bool CanViewMaterialRequestsModule()
    {
        return HasAnyPermission(AppPermissions.MaterialRequestsView, AppPermissions.MaterialRequestsManage);
    }

    private bool CanViewStockModule()
    {
        return HasAnyPermission(AppPermissions.StockView, AppPermissions.StockManage);
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        return User.IsInRole(AppRoles.Admin) || permissions.Any(permission => User.HasClaim(AppClaimTypes.Permission, permission));
    }
}
