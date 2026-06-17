using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewPurchasing)]
public class PurchaseOrdersController : Controller
{
    private const int DefaultListTake = 50;
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
        bool load = true,
        int take = DefaultListTake,
        bool showAll = false,
        bool archivedOnly = false,
        CancellationToken cancellationToken = default)
    {
        take = Math.Max(DefaultListTake, take);

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
        ViewBag.ProjectId = projectId;
        ViewBag.Load = load;
        ViewBag.CurrentTake = take;
        ViewBag.ShowAll = showAll;
        ViewBag.ListAction = archivedOnly ? nameof(Archived) : nameof(Index);
        ViewBag.OrderListTitle = archivedOnly ? "Arşiv siparişler" : "Genel ve proje siparişleri";
        ViewBag.IsArchiveList = archivedOnly;
        await FillLookupsAsync(cancellationToken);
        ViewBag.StatusOptions = Enum.GetValues<PurchaseOrderStatus>()
            .Where(x => archivedOnly || x != PurchaseOrderStatus.Delivered)
            .ToList();

        if (projectId.HasValue)
        {
            var project = await _context.Projects.AsNoTracking().ApplyRecordVisibility(User).FirstOrDefaultAsync(x => x.Id == projectId.Value, cancellationToken);
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

        if (!load)
        {
            ViewBag.IsDeferredLoad = true;
            ViewBag.HasMore = false;
            ViewBag.ResultCount = 0;
            return View(Array.Empty<PurchaseOrder>());
        }

        ViewBag.IsDeferredLoad = false;

        var query = _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking()
            .ApplyRecordVisibility(User, includeArchived: archivedOnly, onlyArchived: archivedOnly);

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId);
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
        else if (!archivedOnly)
        {
            query = query.Where(x => x.Status != PurchaseOrderStatus.Delivered);
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

        List<PurchaseOrder> orders;
        var hasMore = false;

        if (showAll)
        {
            orders = await query.ToListAsync(cancellationToken);
        }
        else
        {
            orders = await query.Take(take + 1).ToListAsync(cancellationToken);
            hasMore = orders.Count > take;
            if (hasMore)
            {
                orders.RemoveAt(orders.Count - 1);
            }
        }

        ViewBag.HasMore = hasMore;
        ViewBag.NextTake = take + DefaultListTake;
        ViewBag.ResultCount = orders.Count;

        return View(orders);
    }

    public async Task<IActionResult> Delivered(
        Guid? projectId,
        string? q,
        PurchaseOrderScope? scope,
        Guid? supplierId,
        Guid? materialId,
        string? requestedByUserId,
        string? requestedBy,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? sort,
        bool load = true,
        int take = DefaultListTake,
        bool showAll = false,
        CancellationToken cancellationToken = default)
    {
        var result = await Index(projectId, q, PurchaseOrderStatus.Delivered, scope, supplierId, materialId, requestedByUserId, requestedBy, dateFrom, dateTo, sort, load, take, showAll, archivedOnly: false, cancellationToken);
        ViewBag.OrderListTitle = "Teslim edilen siparişler";
        ViewBag.ListAction = nameof(Delivered);
        ViewBag.StatusOptions = new List<PurchaseOrderStatus> { PurchaseOrderStatus.Delivered };
        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
    }

    public async Task<IActionResult> Archived(
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
        bool load = true,
        int take = DefaultListTake,
        bool showAll = false,
        CancellationToken cancellationToken = default)
    {
        var result = await Index(projectId, q, status, scope, supplierId, materialId, requestedByUserId, requestedBy, dateFrom, dateTo, sort, load, take, showAll, archivedOnly: true, cancellationToken);
        return result is ViewResult viewResult
            ? View(nameof(Index), viewResult.Model)
            : result;
    }

    public async Task<IActionResult> Details(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetDetailsAsync(id, cancellationToken);
        if (order is null || !order.IsVisibleTo(User))
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> Repeat(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var sourceOrder = await _context.PurchaseOrders
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (sourceOrder is null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);
        var today = DateTime.Today;
        var repeatedOrder = new PurchaseOrder
        {
            ProjectId = sourceOrder.ProjectId,
            SupplierId = sourceOrder.SupplierId,
            MaterialId = sourceOrder.MaterialId,
            OrderNumber = orderNumber,
            Visibility = sourceOrder.Visibility,
            Scope = sourceOrder.Scope,
            TrackingState = 0,
            Content = sourceOrder.Content,
            Quantity = sourceOrder.Quantity,
            QuantityText = sourceOrder.QuantityText,
            Unit = sourceOrder.Unit,
            Quality = sourceOrder.Quality,
            Status = PurchaseOrderStatus.Requested,
            OrderDate = today,
            ExpectedArrivalDate = sourceOrder.ExpectedArrivalDate.HasValue && sourceOrder.ExpectedArrivalDate.Value.Date >= today
                ? sourceOrder.ExpectedArrivalDate
                : null,
            ArrivalDate = null,
            RequestedBy = currentUser?.FullName ?? User.Identity?.Name ?? sourceOrder.RequestedBy,
            RequestedByUserId = currentUser?.Id,
            PaymentTerm = sourceOrder.PaymentTerm,
            UnitPrice = sourceOrder.UnitPrice,
            UnitPriceText = sourceOrder.UnitPriceText,
            OrderTotal = sourceOrder.OrderTotal,
            Currency = sourceOrder.Currency,
            VatRate = sourceOrder.VatRate,
            Notes = sourceOrder.Notes,
            IsActive = sourceOrder.IsActive
        };

        await _purchaseOrderService.CreateAsync(repeatedOrder, cancellationToken);
        await _projectTimelineService.AddForOrderAsync(repeatedOrder.Id, "Sipariş tekrarlandı", $"{repeatedOrder.OrderNumber} - {repeatedOrder.Content}", cancellationToken);
        TempData["Success"] = $"Yeni sipariş oluşturuldu: {orderNumber}";

        return RedirectToLocal(returnUrl);
    }

    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> Create(Guid? projectId, Guid? materialRequestId, string? returnUrl, CancellationToken cancellationToken)
    {
        PurchaseOrder? prefilledOrder = null;

        if (materialRequestId.HasValue)
        {
            var materialRequest = await _context.MaterialRequests
                .Include(x => x.Project)
                .Include(x => x.Material)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == materialRequestId.Value, cancellationToken);
            if (materialRequest is null)
            {
                return NotFound();
            }

            projectId ??= materialRequest.ProjectId;
            prefilledOrder = new PurchaseOrder
            {
                ProjectId = materialRequest.ProjectId,
                MaterialId = materialRequest.MaterialId,
                Content = materialRequest.RequestedItem,
                Quantity = materialRequest.Quantity,
                QuantityText = materialRequest.QuantityText,
                Unit = materialRequest.Unit,
                Quality = materialRequest.Quality,
                ExpectedArrivalDate = materialRequest.NeededBy,
                Notes = materialRequest.Notes,
                Scope = materialRequest.ProjectId.HasValue ? PurchaseOrderScope.Project : PurchaseOrderScope.General
            };
        }

        if (projectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == projectId.Value, cancellationToken);
            if (!canUseProject)
            {
                return NotFound();
            }
        }

        await FillLookupsAsync(cancellationToken);
        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
        return View(prefilledOrder ?? new PurchaseOrder
        {
            ProjectId = projectId,
            OrderNumber = await GenerateOrderNumberAsync(cancellationToken),
            OrderDate = DateTime.Today,
            Scope = projectId.HasValue ? PurchaseOrderScope.Project : PurchaseOrderScope.General
        });
    }

    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> QuickCreate(Guid? projectId, Guid? templateId, string? returnUrl, CancellationToken cancellationToken)
    {
        var model = await BuildQuickCreateModelAsync(projectId, templateId, cancellationToken);
        await FillQuickCreateLookupsAsync(cancellationToken);
        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> QuickCreate(QuickPurchaseOrderViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        if (model.ProjectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == model.ProjectId.Value, cancellationToken);
            if (!canUseProject)
            {
                ModelState.AddModelError(nameof(model.ProjectId), "SeÃ§ilen proje iÃ§in yetkiniz bulunmuyor.");
            }
        }

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
            ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
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
                Visibility = User.NormalizeRecordVisibility(model.Visibility),
                Scope = model.ProjectId.HasValue ? PurchaseOrderScope.Project : model.Scope,
                TrackingState = 0,
                Content = line.Content!.Trim(),
                Quantity = line.Quantity,
                QuantityText = string.IsNullOrWhiteSpace(line.QuantityText) && line.Quantity.HasValue
                    ? string.IsNullOrWhiteSpace(line.Unit)
                        ? line.Quantity.Value.ToString("0.###")
                        : $"{line.Quantity.Value:0.###} {line.Unit.Trim()}"
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
                OrderTotal = line.UnitPrice.HasValue && line.Quantity.HasValue ? line.UnitPrice.Value * line.Quantity.Value : line.OrderTotal,
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
            await _projectTimelineService.AddForOrderAsync(order.Id, "Hızlı sipariş oluşturuldu", $"{order.OrderNumber} - {order.Content}", cancellationToken);
        }

        TempData["Success"] = $"{createdOrders.Count} sipariş oluşturuldu.";
        return RedirectToLocal(returnUrl, model.ProjectId.HasValue ? new { projectId = model.ProjectId } : null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanCreatePurchasing)]
    public async Task<IActionResult> Create(PurchaseOrder order, string? returnUrl, CancellationToken cancellationToken)
    {
        order.Visibility = User.NormalizeRecordVisibility(order.Visibility);
        NormalizePurchaseOrderInput(order);
        if (string.IsNullOrWhiteSpace(order.OrderNumber))
        {
            ModelState.Remove(nameof(order.OrderNumber));
            order.OrderNumber = await GenerateOrderNumberAsync(cancellationToken);
        }

        if (order.ProjectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == order.ProjectId.Value, cancellationToken);
            if (!canUseProject)
            {
                ModelState.AddModelError(nameof(order.ProjectId), "SeÃ§ilen proje iÃ§in yetkiniz bulunmuyor.");
            }
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
            return View(order);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (await _context.PurchaseOrders.AnyAsync(x => x.OrderNumber == order.OrderNumber, cancellationToken))
        {
            order.OrderNumber = await GenerateOrderNumberAsync(cancellationToken);
        }

        order.RequestedBy = currentUser?.FullName ?? User.Identity?.Name;
        order.RequestedByUserId = currentUser?.Id;
        await _purchaseOrderService.CreateAsync(order, cancellationToken);
        await _projectTimelineService.AddForOrderAsync(order.Id, "Sipariş oluşturuldu", $"{order.OrderNumber} - {order.Content}", cancellationToken);
        return RedirectToLocal(returnUrl, order.ProjectId.HasValue ? new { projectId = order.ProjectId } : null);
    }

    [Authorize(Policy = AppPolicies.CanUpdatePurchasing)]
    public async Task<IActionResult> Edit(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var order = await _context.PurchaseOrders
            .Include(x => x.Project)
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || !order.IsVisibleTo(User))
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdatePurchasing)]
    public async Task<IActionResult> Edit(Guid id, PurchaseOrder order, string? returnUrl, CancellationToken cancellationToken)
    {
        if (id != order.Id)
        {
            return BadRequest();
        }

        order.Visibility = User.NormalizeRecordVisibility(order.Visibility);
        NormalizePurchaseOrderInput(order);
        if (order.ProjectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == order.ProjectId.Value, cancellationToken);
            if (!canUseProject)
            {
                ModelState.AddModelError(nameof(order.ProjectId), "SeÃ§ilen proje iÃ§in yetkiniz bulunmuyor.");
            }
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl);
            return View(order);
        }

        var oldStatus = await _context.PurchaseOrders
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x => x.Id == id)
            .Select(x => (PurchaseOrderStatus?)x.Status)
            .FirstOrDefaultAsync(cancellationToken);
        await _purchaseOrderService.UpdateAsync(order, cancellationToken);
        var updateTitle = oldStatus.HasValue && oldStatus.Value != order.Status ? "Sipariş durumu değişti" : "Sipariş güncellendi";
        await _projectTimelineService.AddForOrderAsync(order.Id, updateTitle, $"{order.OrderNumber} - {order.Content} - {order.Status.ToDisplayName()}", cancellationToken);
        return RedirectToLocal(returnUrl, order.ProjectId.HasValue ? new { projectId = order.ProjectId } : null);
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderService.GetDetailsAsync(id, cancellationToken);
        if (order is not null && !order.IsVisibleTo(User))
        {
            return NotFound();
        }
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
        var order = await _context.PurchaseOrders
            .Include(x => x.Project)
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null || !order.IsVisibleTo(User))
        {
            return NotFound();
        }

        if (order.Status != status)
        {
            order.Status = status;
            order.ArrivalDate = status == PurchaseOrderStatus.Delivered ? DateTime.Today : order.ArrivalDate;
            await _context.SaveChangesAsync(cancellationToken);
            await _projectTimelineService.AddForOrderAsync(order.Id, "Sipariş durumu değişti", $"{order.OrderNumber} - {order.Content} - {status.ToDisplayName()}", cancellationToken);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdatePurchasing)]
    public async Task<IActionResult> Archive(Guid id, bool archived = true, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var order = await _context.PurchaseOrders
            .Include(x => x.Project)
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        SetOrderArchiveState(order, archived);
        await _context.SaveChangesAsync(cancellationToken);
        await _projectTimelineService.AddForOrderAsync(order.Id, archived ? "Sipariş arşivlendi" : "Sipariş arşivden çıkarıldı", $"{order.OrderNumber} - {order.Content}", cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanUpdatePurchasing)]
    public async Task<IActionResult> BulkArchive(Guid[] ids, bool archived = true, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var orders = await _context.PurchaseOrders
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            SetOrderArchiveState(order, archived);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanChangePurchasingStatus)]
    public async Task<IActionResult> BulkUpdateStatus(Guid[] ids, PurchaseOrderStatus status, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count == 0)
        {
            return RedirectToLocal(returnUrl);
        }

        var orders = await _context.PurchaseOrders
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: false)
            .Where(x => selectedIds.Contains(x.Id) && !x.IsArchived)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            order.Status = status;
            order.ArrivalDate = status == PurchaseOrderStatus.Delivered ? DateTime.Today : null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToLocal(returnUrl);
    }

    [Authorize(Policy = AppPolicies.CanViewPurchasing)]
    public async Task<IActionResult> DownloadArchive(CancellationToken cancellationToken)
    {
        var rows = await _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking()
            .ApplyRecordVisibility(User, includeArchived: true, onlyArchived: true)
            .OrderByDescending(x => x.ArchivedAt ?? x.UpdatedAt ?? x.CreatedAt)
            .Select(x => (IReadOnlyList<object?>)new object?[]
            {
                x.OrderNumber,
                x.Content,
                x.Project != null ? $"{x.Project.Code} - {x.Project.Name}" : null,
                x.Supplier != null ? x.Supplier.Name : null,
                x.Material != null ? x.Material.Name : null,
                x.Status.ToDisplayName(),
                x.QuantityText,
                x.OrderDate,
                x.ExpectedArrivalDate,
                x.ArrivalDate,
                x.ArchivedAt,
                x.Notes
            })
            .ToListAsync(cancellationToken);

        return ExcelFile(
            [new ExcelSheet("Arşiv Siparişler", ["Sipariş No", "İçerik", "Proje", "Tedarikçi", "Malzeme", "Durum", "Miktar", "Sipariş Tarihi", "Beklenen", "Teslim", "Arşiv Tarihi", "Not"], rows)],
            $"arsiv-siparisler-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Projects = await _context.Projects
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
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
        if (projectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == projectId.Value, cancellationToken);
            if (!canUseProject)
            {
                projectId = null;
            }
        }

        var model = new QuickPurchaseOrderViewModel
        {
            TemplateId = templateId,
            ProjectId = projectId,
            Visibility = RecordVisibility.General,
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

    private IActionResult RedirectToLocal(string? returnUrl, object? fallbackRouteValues = null)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index), fallbackRouteValues);
    }

    private string? NormalizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    private static void NormalizePurchaseOrderInput(PurchaseOrder order)
    {
        order.Content = order.Content?.Trim() ?? string.Empty;
        order.Unit = string.IsNullOrWhiteSpace(order.Unit) ? null : order.Unit.Trim();
        order.Quality = string.IsNullOrWhiteSpace(order.Quality) ? null : order.Quality.Trim();
        order.PaymentTerm = string.IsNullOrWhiteSpace(order.PaymentTerm) ? null : order.PaymentTerm.Trim();
        order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? null : order.Notes.Trim();
        order.Currency = string.IsNullOrWhiteSpace(order.Currency) ? "TRY" : order.Currency.Trim().ToUpperInvariant();
        order.Scope = order.ProjectId.HasValue ? PurchaseOrderScope.Project : order.Scope;

        if (order.Quantity.HasValue)
        {
            order.QuantityText = string.IsNullOrWhiteSpace(order.Unit)
                ? order.Quantity.Value.ToString("0.###")
                : $"{order.Quantity.Value:0.###} {order.Unit}";
        }
        else
        {
            order.QuantityText = string.IsNullOrWhiteSpace(order.QuantityText) ? null : order.QuantityText.Trim();
        }

        order.UnitPriceText = order.UnitPrice.HasValue
            ? $"{order.UnitPrice.Value:N2} {order.Currency}"
            : null;

        order.OrderTotal = order.UnitPrice.HasValue && order.Quantity.HasValue
            ? order.UnitPrice.Value * order.Quantity.Value
            : order.OrderTotal;
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

    private static void SetOrderArchiveState(PurchaseOrder order, bool archived)
    {
        order.IsArchived = archived;
        order.ArchivedAt = archived ? DateTime.UtcNow : null;
    }

    private FileContentResult ExcelFile(IReadOnlyList<ExcelSheet> sheets, string fileName)
    {
        var bytes = ExcelWorkbookBuilder.Build(sheets);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
