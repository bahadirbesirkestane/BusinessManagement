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

[Authorize(Policy = AppPolicies.CanViewPurchasing)]
public class PurchaseOrdersController : Controller
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILookupService _lookupService;
    private readonly IRecordActivityService _recordActivityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectTimelineService _projectTimelineService;
    private readonly IPurchaseOrderTemplateService _purchaseOrderTemplateService;
    private readonly ApplicationDbContext _context;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        ILookupService lookupService,
        IRecordActivityService recordActivityService,
        UserManager<ApplicationUser> userManager,
        IProjectTimelineService projectTimelineService,
        IPurchaseOrderTemplateService purchaseOrderTemplateService,
        ApplicationDbContext context)
    {
        _purchaseOrderService = purchaseOrderService;
        _lookupService = lookupService;
        _recordActivityService = recordActivityService;
        _userManager = userManager;
        _projectTimelineService = projectTimelineService;
        _purchaseOrderTemplateService = purchaseOrderTemplateService;
        _context = context;
    }

    public async Task<IActionResult> Index(
        Guid? projectId,
        string? q,
        PurchaseOrderStatus? status,
        PurchaseOrderScope? scope,
        Guid? supplierId,
        Guid? materialId,
        string? requestedByUserId,
        string? requestedBy,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? sort,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking();

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId);
            ViewBag.ProjectId = projectId;
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.OrderNumber.Contains(term) ||
                x.Content.Contains(term) ||
                (x.QuantityText != null && x.QuantityText.Contains(term)) ||
                (x.Quality != null && x.Quality.Contains(term)) ||
                (x.RequestedBy != null && x.RequestedBy.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)) ||
                (x.Supplier != null && x.Supplier.Name.Contains(term)) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))) ||
                (x.Material != null && x.Material.Name.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (scope.HasValue)
        {
            query = query.Where(x => x.Scope == scope.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == supplierId.Value);
        }

        if (materialId.HasValue)
        {
            query = query.Where(x => x.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(requestedByUserId))
        {
            var requester = await _userManager.FindByIdAsync(requestedByUserId);
            var requesterName = requester?.FullName;
            var requesterEmail = requester?.Email;
            query = query.Where(x =>
                x.RequestedByUserId == requestedByUserId ||
                (requesterName != null && x.RequestedBy == requesterName) ||
                (requesterEmail != null && x.RequestedBy == requesterEmail));
        }

        if (!string.IsNullOrWhiteSpace(requestedBy))
        {
            var requesterTerm = requestedBy.Trim();
            query = query.Where(x => x.RequestedBy != null && x.RequestedBy.Contains(requesterTerm));
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.OrderDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.OrderDate <= dateTo.Value);
        }

        query = sort switch
        {
            "orderNumber" => query.OrderBy(x => x.OrderNumber),
            "content" => query.OrderBy(x => x.Content),
            "project" => query.OrderBy(x => x.Project == null ? string.Empty : x.Project.Code),
            "supplier" => query.OrderBy(x => x.Supplier == null ? string.Empty : x.Supplier.Name),
            "requester" => query.OrderBy(x => x.RequestedBy),
            "status" => query.OrderBy(x => x.Status),
            "date" => query.OrderBy(x => x.OrderDate),
            "arrival" => query.OrderBy(x => x.ExpectedArrivalDate ?? DateTime.MaxValue),
            "total" => query.OrderByDescending(x => x.OrderTotal ?? ((x.UnitPrice ?? 0) * (x.Quantity ?? 0))),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.FilterScope = scope;
        ViewBag.FilterSupplierId = supplierId;
        ViewBag.FilterMaterialId = materialId;
        ViewBag.FilterRequestedByUserId = requestedByUserId;
        ViewBag.FilterRequestedBy = requestedBy;
        ViewBag.FilterDateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.FilterDateTo = dateTo?.ToString("yyyy-MM-dd");
        ViewBag.Sort = sort;
        await FillLookupsAsync(cancellationToken);
        if (projectId.HasValue)
        {
            var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId.Value, cancellationToken);
            if (project is not null)
            {
                ViewBag.Breadcrumbs = new Dictionary<string, string?>
                {
                    ["Projeler"] = Url.Action("Index", "Projects"),
                    [project.Code] = Url.Action("Details", "Projects", new { id = project.Id }),
                    ["Siparişler"] = null
                };
            }
        }

        return View(await query.ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetDetailsAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var requesterName = order.RequestedBy;
        if (string.IsNullOrWhiteSpace(requesterName) && !string.IsNullOrWhiteSpace(order.RequestedByUserId))
        {
            var requester = await _userManager.FindByIdAsync(order.RequestedByUserId);
            requesterName = requester?.FullName ?? requester?.Email ?? requester?.UserName;
        }

        ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        ViewBag.RequesterName = requesterName;
        ViewBag.Breadcrumbs = order.Project is not null
            ? new Dictionary<string, string?>
            {
                ["Projeler"] = Url.Action("Index", "Projects"),
                [order.Project.Code] = Url.Action("Details", "Projects", new { id = order.Project.Id }),
                ["Siparişler"] = Url.Action(nameof(Index), new { projectId = order.Project.Id }),
                [order.OrderNumber] = null
            }
            : new Dictionary<string, string?>
            {
                ["Siparişler"] = Url.Action(nameof(Index)),
                [order.OrderNumber] = null
            };
        ViewBag.Activity = new RecordActivityViewModel
        {
            OwnerType = RecordOwnerType.PurchaseOrder,
            OwnerId = order.Id,
            Comments = await _recordActivityService.GetCommentsAsync(RecordOwnerType.PurchaseOrder, order.Id, cancellationToken),
            Files = await _recordActivityService.GetFilesAsync(RecordOwnerType.PurchaseOrder, order.Id, cancellationToken)
        };

        return View(order);
    }

    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> Create(Guid? projectId, CancellationToken cancellationToken)
    {
        await FillLookupsAsync(cancellationToken);
        return View(new PurchaseOrder
        {
            ProjectId = projectId,
            OrderNumber = await GenerateOrderNumberAsync(cancellationToken),
            OrderDate = DateTime.Today,
            Scope = projectId.HasValue ? PurchaseOrderScope.Project : PurchaseOrderScope.General
        });
    }

    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> QuickCreate(Guid? projectId, Guid? templateId, CancellationToken cancellationToken)
    {
        var model = await BuildQuickCreateModelAsync(projectId, templateId, cancellationToken);
        await FillQuickCreateLookupsAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> QuickCreate(QuickPurchaseOrderViewModel model, CancellationToken cancellationToken)
    {
        var validLines = model.Lines
            .Where(x => !string.IsNullOrWhiteSpace(x.Content))
            .ToList();

        if (validLines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Kaydedilecek en az bir sipariş satırı girin.");
        }

        if (!ModelState.IsValid)
        {
            EnsureQuickRows(model);
            await FillQuickCreateLookupsAsync(cancellationToken);
            return View(model);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var requestedBy = currentUser?.FullName ?? User.Identity?.Name;
        var createdOrders = new List<PurchaseOrder>();
        var reservedOrderNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in validLines)
        {
            var orderNumber = await GenerateOrderNumberAsync(cancellationToken, reservedOrderNumbers);
            reservedOrderNumbers.Add(orderNumber);

            var order = new PurchaseOrder
            {
                ProjectId = model.ProjectId,
                SupplierId = model.SupplierId,
                MaterialId = line.MaterialId,
                OrderNumber = orderNumber,
                Scope = model.ProjectId.HasValue ? PurchaseOrderScope.Project : model.Scope,
                TrackingState = 0,
                Content = line.Content!.Trim(),
                Quantity = line.Quantity,
                QuantityText = string.IsNullOrWhiteSpace(line.QuantityText) && line.Quantity.HasValue
                    ? line.Quantity.Value.ToString("0.###")
                    : line.QuantityText?.Trim(),
                Unit = line.Unit?.Trim(),
                Quality = line.Quality?.Trim(),
                Status = model.Status,
                OrderDate = model.OrderDate,
                ExpectedArrivalDate = model.ExpectedArrivalDate,
                RequestedBy = requestedBy,
                RequestedByUserId = currentUser?.Id,
                PaymentTerm = model.PaymentTerm?.Trim(),
                UnitPrice = line.UnitPrice,
                UnitPriceText = line.UnitPrice.HasValue ? $"{line.UnitPrice.Value:N2} {model.Currency}" : null,
                OrderTotal = line.OrderTotal ?? (line.UnitPrice.HasValue && line.Quantity.HasValue ? line.UnitPrice.Value * line.Quantity.Value : null),
                Currency = string.IsNullOrWhiteSpace(model.Currency) ? "TRY" : model.Currency.Trim().ToUpperInvariant(),
                Notes = line.Notes?.Trim(),
                IsActive = true
            };

            _context.PurchaseOrders.Add(order);
            createdOrders.Add(order);
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var order in createdOrders)
        {
            await _projectTimelineService.AddForOrderAsync(order.Id, "Hızlı sipariş oluşturuldu", order.Content, cancellationToken);
        }

        TempData["Success"] = $"{createdOrders.Count} sipariş oluşturuldu.";
        return RedirectToAction(nameof(Index), model.ProjectId.HasValue ? new { projectId = model.ProjectId } : null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> Create(PurchaseOrder order, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(order);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (string.IsNullOrWhiteSpace(order.OrderNumber) || await _context.PurchaseOrders.AnyAsync(x => x.OrderNumber == order.OrderNumber, cancellationToken))
        {
            order.OrderNumber = await GenerateOrderNumberAsync(cancellationToken);
        }

        order.RequestedBy = currentUser?.FullName ?? User.Identity?.Name;
        order.RequestedByUserId = currentUser?.Id;
        await _purchaseOrderService.CreateAsync(order, cancellationToken);
        await _projectTimelineService.AddForOrderAsync(order.Id, "Sipariş oluşturuldu", order.Content, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanUpdatePurchasing)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdatePurchasing)]
    public async Task<IActionResult> Edit(Guid id, PurchaseOrder order, CancellationToken cancellationToken)
    {
        if (id != order.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(order);
        }

        var oldStatus = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => (PurchaseOrderStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken);
        await _purchaseOrderService.UpdateAsync(order, cancellationToken);
        var updateTitle = oldStatus.HasValue && oldStatus.Value != order.Status ? "Sipariş durumu değişti" : "Sipariş güncellendi";
        await _projectTimelineService.AddForOrderAsync(order.Id, updateTitle, $"{order.Content} - {order.Status.ToDisplayName()}", cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetDetailsAsync(id, cancellationToken);
        ViewBag.ReturnUrl = returnUrl;
        return order is null ? NotFound() : View(order);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var linkedCosts = await _context.ProjectCostItems.Where(x => x.PurchaseOrderId == id).ToListAsync(cancellationToken);
        foreach (var cost in linkedCosts)
        {
            cost.PurchaseOrderId = null;
        }

        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.PurchaseOrder && x.OwnerId == id));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.PurchaseOrder && x.OwnerId == id));
        await _context.SaveChangesAsync(cancellationToken);
        await _purchaseOrderService.DeleteAsync(id, cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        if (ids.Length == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var selectedIds = ids.Distinct().ToList();
        var linkedCosts = await _context.ProjectCostItems.Where(x => x.PurchaseOrderId.HasValue && selectedIds.Contains(x.PurchaseOrderId.Value)).ToListAsync(cancellationToken);
        foreach (var cost in linkedCosts)
        {
            cost.PurchaseOrderId = null;
        }

        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.PurchaseOrder && selectedIds.Contains(x.OwnerId)));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.PurchaseOrder && selectedIds.Contains(x.OwnerId)));
        _context.PurchaseOrders.RemoveRange(_context.PurchaseOrders.Where(x => selectedIds.Contains(x.Id)));
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangePurchasingStatus)]
    public async Task<IActionResult> UpdateStatus(Guid id, PurchaseOrderStatus status, string? returnUrl, CancellationToken cancellationToken)
    {
        var order = await _context.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != status)
        {
            order.Status = status;
            order.ArrivalDate = status == PurchaseOrderStatus.Delivered ? DateTime.Today : order.ArrivalDate;
            await _context.SaveChangesAsync(cancellationToken);
            await _projectTimelineService.AddForOrderAsync(order.Id, "Sipariş durumu değişti", $"{order.Content} - {status.ToDisplayName()}", cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Projects = await _lookupService.GetProjectsAsync(cancellationToken);
        ViewBag.Suppliers = await _lookupService.GetSuppliersAsync(cancellationToken);
        ViewBag.Materials = await _lookupService.GetMaterialsAsync(cancellationToken);
        ViewBag.Users = await _userManager.Users
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);
    }

    private async Task FillQuickCreateLookupsAsync(CancellationToken cancellationToken)
    {
        await FillLookupsAsync(cancellationToken);
        ViewBag.Templates = await _context.PurchaseOrderTemplates
            .AsNoTracking()
            .Include(x => x.DefaultSupplier)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                Text = string.IsNullOrWhiteSpace(x.Code)
                    ? x.Name
                    : x.Code + " - " + x.Name,
                SupplierName = x.DefaultSupplier != null ? x.DefaultSupplier.Name : null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<QuickPurchaseOrderViewModel> BuildQuickCreateModelAsync(Guid? projectId, Guid? templateId, CancellationToken cancellationToken)
    {
        var model = new QuickPurchaseOrderViewModel
        {
            TemplateId = templateId,
            ProjectId = projectId,
            Scope = projectId.HasValue ? PurchaseOrderScope.Project : PurchaseOrderScope.General,
            OrderDate = DateTime.Today,
            Currency = "TRY"
        };

        if (!templateId.HasValue)
        {
            return model;
        }

        var template = await _purchaseOrderTemplateService.GetTemplateWithLinesAsync(templateId.Value, cancellationToken);
        if (template is null || !template.IsActive)
        {
            return model;
        }

        model.SupplierId = template.DefaultSupplierId;
        model.Scope = projectId.HasValue ? PurchaseOrderScope.Project : template.DefaultScope;
        model.Status = template.DefaultStatus;
        model.Currency = string.IsNullOrWhiteSpace(template.DefaultCurrency) ? "TRY" : template.DefaultCurrency;
        model.PaymentTerm = template.DefaultPaymentTerm;
        model.Lines = template.Lines
            .OrderBy(x => x.SortOrder)
            .Select(x => new QuickPurchaseOrderLineViewModel
            {
                MaterialId = x.MaterialId,
                Content = x.Content,
                Quantity = x.Quantity,
                QuantityText = x.QuantityText,
                Unit = x.Unit,
                Quality = x.Quality,
                UnitPrice = x.UnitPrice,
                OrderTotal = x.OrderTotal,
                Notes = x.Notes
            })
            .ToList();

        EnsureQuickRows(model);
        return model;
    }

    private static void EnsureQuickRows(QuickPurchaseOrderViewModel model)
    {
        while (model.Lines.Count < 2)
        {
            model.Lines.Add(new QuickPurchaseOrderLineViewModel());
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken, ISet<string>? reservedOrderNumbers = null)
    {
        var baseNumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}";
        var orderNumber = baseNumber;
        var sequence = 1;

        while ((reservedOrderNumbers?.Contains(orderNumber) ?? false) ||
               await _context.PurchaseOrders.AnyAsync(x => x.OrderNumber == orderNumber, cancellationToken))
        {
            orderNumber = $"{baseNumber}-{sequence:00}";
            sequence++;
        }

        return orderNumber;
    }
}
