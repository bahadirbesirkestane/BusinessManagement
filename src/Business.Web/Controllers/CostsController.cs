using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
using Business.Web.Services;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewCosts)]
public class CostsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProjectTimelineService _projectTimelineService;

    public CostsController(ApplicationDbContext context, IProjectTimelineService projectTimelineService)
    {
        _context = context;
        _projectTimelineService = projectTimelineService;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Projects));
    }

    public async Task<IActionResult> Projects(string? q, Guid? customerId, string? currency, CancellationToken cancellationToken)
    {
        ViewBag.FilterQ = q;
        ViewBag.FilterCustomerId = customerId;
        ViewBag.FilterCurrency = currency;
        ViewBag.Customers = await _context.Customers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Currencies = CurrencyMetadata.GetSelectList();

        var projects = await BuildProjectsQuery(q, customerId, currency)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            project.CostItems = project.CostItems.Where(x => x.IsVisibleTo(User)).ToList();
            project.PurchaseOrders = project.PurchaseOrders.Where(x => x.IsVisibleTo(User)).ToList();
        }

        return View(projects);
    }

    public async Task<FileContentResult> ExportProjects(string? q, Guid? customerId, string? currency, CancellationToken cancellationToken)
    {
        var canViewBudget = CanViewProjectBudget();
        var projects = await BuildProjectsQuery(q, customerId, currency)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            project.CostItems = project.CostItems.Where(x => x.IsVisibleTo(User)).ToList();
            project.PurchaseOrders = project.PurchaseOrders.Where(x => x.IsVisibleTo(User)).ToList();
        }

        var headers = new List<string> { "Kod", "Proje", "Müşteri" };
        if (canViewBudget)
        {
            headers.Add("Bütçe");
        }

        headers.AddRange(["Maliyet Kalemleri", "Sipariş Tutarı", "Toplam", "Eksik Kur"]);

        var rows = projects.Select(project =>
        {
            var summary = CostSummaryBuilder.Build(project);
            var row = new List<object?>
            {
                project.Code,
                project.Name,
                project.Customer?.Name ?? project.CustomerName ?? "-"
            };

            if (canViewBudget)
            {
                row.Add(summary.Budget.HasValue ? $"{summary.Budget.Value:N2} {summary.ProjectCurrency}" : "-");
            }

            row.Add(FormatCurrencyTotals(summary.CostItemTotals));
            row.Add(FormatCurrencyTotals(summary.PurchaseOrderTotals));
            row.Add(summary.GrandTotalInProjectCurrency.HasValue
                ? $"{summary.GrandTotalInProjectCurrency.Value:N2} {summary.ProjectCurrency} ({FormatCurrencyTotals(summary.CombinedTotals)})"
                : FormatCurrencyTotals(summary.CombinedTotals));
            row.Add(summary.HasMissingExchangeRates ? string.Join(", ", summary.MissingRateCurrencies) : string.Empty);
            return (IReadOnlyList<object?>)row;
        }).ToList();

        return ExcelFile(
            [new ExcelSheet("Proje Maliyetleri", headers, rows)],
            $"proje-maliyetleri-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var project = await BuildProjectDetailsQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (project is null)
        {
            return NotFound();
        }

        project.CostItems = project.CostItems
            .Where(x => x.IsVisibleTo(User))
            .OrderByDescending(x => x.CostDate ?? x.CreatedAt)
            .ToList();
        project.PurchaseOrders = project.PurchaseOrders
            .Where(x => x.IsVisibleTo(User))
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToList();

        return View(project);
    }

    public async Task<FileContentResult> ExportDetails(Guid id, CancellationToken cancellationToken)
    {
        var project = await BuildProjectDetailsQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Proje bulunamadı.");
        }

        project.CostItems = project.CostItems
            .Where(x => x.IsVisibleTo(User))
            .OrderByDescending(x => x.CostDate ?? x.CreatedAt)
            .ToList();
        project.PurchaseOrders = project.PurchaseOrders
            .Where(x => x.IsVisibleTo(User))
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToList();

        var summary = CostSummaryBuilder.Build(project);
        var canViewBudget = CanViewProjectBudget();
        var summaryRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Proje", $"{project.Code} - {project.Name}" },
            new object?[] { "Müşteri", project.Customer?.Name ?? project.CustomerName ?? "-" },
            new object?[] { "Proje para birimi", summary.ProjectCurrency },
            new object?[] { "Elle girilen maliyetler", FormatCurrencyTotals(summary.CostItemTotals) },
            new object?[] { "Sipariş maliyetleri", FormatCurrencyTotals(summary.PurchaseOrderTotals) },
            new object?[] { "Toplam maliyet", FormatCurrencyTotals(summary.CombinedTotals) },
            new object?[] { "EUR/TRY", summary.EurToTryRate?.ToString("N4") ?? "-" },
            new object?[] { "USD/TRY", summary.UsdToTryRate?.ToString("N4") ?? "-" },
            new object?[] { "Dönüştürülmüş toplam", summary.GrandTotalInProjectCurrency.HasValue ? $"{summary.GrandTotalInProjectCurrency.Value:N2} {summary.ProjectCurrency}" : "-" },
            new object?[] { "Eksik kur", summary.HasMissingExchangeRates ? string.Join(", ", summary.MissingRateCurrencies) : string.Empty }
        };

        if (canViewBudget)
        {
            summaryRows.Insert(3, new object?[] { "Bütçe", summary.Budget.HasValue ? $"{summary.Budget.Value:N2} {summary.ProjectCurrency}" : "-" });
            summaryRows.Add(new object?[] { "Bütçe farkı", summary.RemainingBudgetInProjectCurrency.HasValue ? $"{summary.RemainingBudgetInProjectCurrency.Value:N2} {summary.ProjectCurrency}" : "-" });
        }

        var orderRows = project.PurchaseOrders.Select(order => (IReadOnlyList<object?>)new object?[]
        {
            order.OrderDate?.ToString("dd.MM.yyyy") ?? order.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"),
            order.OrderNumber,
            order.Supplier?.Name ?? "-",
            order.Material?.Name ?? "-",
            order.Status.ToDisplayName(),
            order.Content,
            order.QuantityText ?? order.Quantity?.ToString("N2") ?? "-",
            order.UnitPriceText ?? order.UnitPrice?.ToString("N2") ?? "-",
            $"{CostSummaryBuilder.GetOrderAmount(order):N2} {CurrencyMetadata.NormalizeStored(order.Currency)}"
        }).ToList();

        var costRows = project.CostItems.Select(item => (IReadOnlyList<object?>)new object?[]
        {
            item.CostDate?.ToString("dd.MM.yyyy") ?? item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"),
            item.Type.ToDisplayName(),
            item.Description,
            item.PurchaseOrder?.OrderNumber ?? "-",
            $"{item.Amount:N2} {CurrencyMetadata.NormalizeStored(item.Currency)}",
            item.Notes ?? "-"
        }).ToList();

        return ExcelFile(
            [
                new ExcelSheet("Özet", ["Alan", "Değer"], summaryRows),
                new ExcelSheet("Sipariş Maliyetleri", ["Tarih", "Sipariş", "Tedarikçi", "Malzeme", "Durum", "İçerik", "Miktar", "Birim Fiyat", "Tutar"], orderRows),
                new ExcelSheet("Maliyet Kalemleri", ["Tarih", "Tür", "Açıklama", "Bağlı Sipariş", "Tutar", "Not"], costRows)
            ],
            $"proje-maliyet-detay-{project.Code}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> PurchaseOrders(string? q, Guid? projectId, Guid? supplierId, Guid? materialId, string? currency, CancellationToken cancellationToken)
    {
        ViewBag.FilterQ = q;
        ViewBag.FilterProjectId = projectId;
        ViewBag.FilterSupplierId = supplierId;
        ViewBag.FilterMaterialId = materialId;
        ViewBag.FilterCurrency = currency;
        ViewBag.Projects = await _context.Projects.AsNoTracking().ApplyRecordVisibility(User).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        ViewBag.Suppliers = await _context.Suppliers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Materials = await _context.Materials.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.Currencies = CurrencyMetadata.GetSelectList();

        var orders = await BuildPurchaseOrdersQuery(q, projectId, supplierId, materialId, currency)
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    public async Task<FileContentResult> ExportPurchaseOrders(string? q, Guid? projectId, Guid? supplierId, Guid? materialId, string? currency, CancellationToken cancellationToken)
    {
        var orders = await BuildPurchaseOrdersQuery(q, projectId, supplierId, materialId, currency)
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        var rows = orders.Select(order => (IReadOnlyList<object?>)new object?[]
        {
            order.OrderDate?.ToString("dd.MM.yyyy") ?? order.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"),
            order.OrderNumber,
            order.Project?.Code ?? "Genel",
            order.Project?.Name ?? "-",
            order.Supplier?.Name ?? "-",
            order.Material?.Name ?? "-",
            order.Status.ToDisplayName(),
            order.Content,
            order.QuantityText ?? order.Quantity?.ToString("N2") ?? "-",
            order.UnitPriceText ?? order.UnitPrice?.ToString("N2") ?? "-",
            $"{CostSummaryBuilder.GetOrderAmount(order):N2} {CurrencyMetadata.NormalizeStored(order.Currency)}"
        }).ToList();

        return ExcelFile(
            [new ExcelSheet("Sipariş Maliyetleri", ["Tarih", "Sipariş", "Proje Kodu", "Proje", "Tedarikçi", "Malzeme", "Durum", "İçerik", "Miktar", "Birim Fiyat", "Tutar"], rows)],
            $"siparis-maliyetleri-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> Materials(string? q, string? currency, CancellationToken cancellationToken)
    {
        ViewBag.FilterQ = q;
        ViewBag.FilterCurrency = currency;
        ViewBag.Currencies = CurrencyMetadata.GetSelectList();

        var orders = await BuildMaterialOrdersQuery(q, currency)
            .OrderBy(x => x.Material == null ? string.Empty : x.Material.Name)
            .ThenByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    public async Task<FileContentResult> ExportMaterials(string? q, string? currency, CancellationToken cancellationToken)
    {
        var orders = await BuildMaterialOrdersQuery(q, currency)
            .OrderBy(x => x.Material == null ? string.Empty : x.Material.Name)
            .ThenByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        var rows = orders.Select(order => (IReadOnlyList<object?>)new object?[]
        {
            order.Material?.Name ?? "Malzeme seçilmedi",
            order.OrderDate?.ToString("dd.MM.yyyy") ?? order.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"),
            order.OrderNumber,
            order.Project?.Code ?? "Genel",
            order.Supplier?.Name ?? "-",
            order.Content,
            order.QuantityText ?? order.Quantity?.ToString("N2") ?? "-",
            order.UnitPriceText ?? order.UnitPrice?.ToString("N2") ?? "-",
            $"{CostSummaryBuilder.GetOrderAmount(order):N2} {CurrencyMetadata.NormalizeStored(order.Currency)}"
        }).ToList();

        return ExcelFile(
            [new ExcelSheet("Malzeme Giderleri", ["Malzeme", "Tarih", "Sipariş", "Proje", "Tedarikçi", "İçerik", "Miktar", "Birim Fiyat", "Tutar"], rows)],
            $"malzeme-giderleri-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    public async Task<IActionResult> General(string? q, Guid? projectId, string? currency, CancellationToken cancellationToken)
    {
        ViewBag.FilterQ = q;
        ViewBag.FilterProjectId = projectId;
        ViewBag.FilterCurrency = currency;
        ViewBag.Projects = await _context.Projects.AsNoTracking().ApplyRecordVisibility(User).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        ViewBag.Currencies = CurrencyMetadata.GetSelectList();

        var items = await BuildGeneralCostsQuery(q, projectId, currency)
            .OrderByDescending(x => x.CostDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(items);
    }

    public async Task<FileContentResult> ExportGeneral(string? q, Guid? projectId, string? currency, CancellationToken cancellationToken)
    {
        var items = await BuildGeneralCostsQuery(q, projectId, currency)
            .OrderByDescending(x => x.CostDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        var rows = items.Select(item => (IReadOnlyList<object?>)new object?[]
        {
            item.CostDate?.ToString("dd.MM.yyyy") ?? item.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy"),
            item.Project?.Code ?? "Genel",
            item.Project?.Name ?? "-",
            item.Type.ToDisplayName(),
            item.Description,
            $"{item.Amount:N2} {CurrencyMetadata.NormalizeStored(item.Currency)}",
            item.PurchaseOrder?.OrderNumber ?? "-",
            item.Notes ?? "-"
        }).ToList();

        return ExcelFile(
            [new ExcelSheet("Genel Giderler", ["Tarih", "Proje Kodu", "Proje", "Tür", "Açıklama", "Tutar", "Bağlı Sipariş", "Not"], rows)],
            $"genel-giderler-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");
    }

    [Authorize(Policy = AppPolicies.CanManageCosts)]
    public async Task<IActionResult> Create(Guid? projectId, CancellationToken cancellationToken)
    {
        await FillLookupsAsync(projectId, cancellationToken);
        return View(new ProjectCostItem
        {
            ProjectId = projectId,
            Type = CostItemType.Overhead,
            CostDate = DateTime.Today,
            Currency = "TRY"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCosts)]
    public async Task<IActionResult> Create(ProjectCostItem item, CancellationToken cancellationToken)
    {
        item.Visibility = User.NormalizeRecordVisibility(item.Visibility);
        await ValidateCostRelationsAsync(item, cancellationToken);

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(item.ProjectId, cancellationToken);
            return View(item);
        }

        NormalizeCostItem(item);
        _context.ProjectCostItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        if (item.ProjectId.HasValue)
        {
            await _projectTimelineService.AddAsync(item.ProjectId.Value, "Maliyet kalemi eklendi", $"{item.Description} - {item.Amount:N2} {item.Currency}", cancellationToken);
            return RedirectToAction(nameof(Details), new { id = item.ProjectId.Value });
        }

        return RedirectToAction(nameof(General));
    }

    [Authorize(Policy = AppPolicies.CanManageCosts)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.ProjectCostItems
            .Include(x => x.Project)
            .Include(x => x.PurchaseOrder)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null || !item.IsVisibleTo(User))
        {
            return NotFound();
        }

        await FillLookupsAsync(item.ProjectId, cancellationToken);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCosts)]
    public async Task<IActionResult> Edit(Guid id, ProjectCostItem item, CancellationToken cancellationToken)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }

        item.Visibility = User.NormalizeRecordVisibility(item.Visibility);
        await ValidateCostRelationsAsync(item, cancellationToken);

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(item.ProjectId, cancellationToken);
            return View(item);
        }

        NormalizeCostItem(item);
        _context.ProjectCostItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
        if (item.ProjectId.HasValue)
        {
            await _projectTimelineService.AddAsync(item.ProjectId.Value, "Maliyet kalemi güncellendi", $"{item.Description} - {item.Amount:N2} {item.Currency}", cancellationToken);
            return RedirectToAction(nameof(Details), new { id = item.ProjectId.Value });
        }

        return RedirectToAction(nameof(General));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCosts)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.ProjectCostItems
            .Include(x => x.Project)
            .Include(x => x.PurchaseOrder)
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null || !item.IsVisibleTo(User))
        {
            return NotFound();
        }

        var projectId = item.ProjectId;
        _context.ProjectCostItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        if (projectId.HasValue)
        {
            await _projectTimelineService.AddAsync(projectId.Value, "Maliyet kalemi silindi", item.Description, cancellationToken);
            return RedirectToAction(nameof(Details), new { id = projectId.Value });
        }

        return RedirectToAction(nameof(General));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageCosts)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count > 0)
        {
            var visibleItems = await _context.ProjectCostItems
                .Include(x => x.Project)
                .Include(x => x.PurchaseOrder)
                .ApplyRecordVisibility(User)
                .Where(x => selectedIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            _context.ProjectCostItems.RemoveRange(visibleItems);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(General));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageProjectBudget)]
    public async Task<IActionResult> UpdateBudgetSettings(Guid id, decimal? budget, string? currency, decimal? eurToTryRate, decimal? usdToTryRate, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .ApplyRecordVisibility(User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        project.Budget = budget;
        project.Currency = CurrencyMetadata.NormalizeInput(currency);
        project.EurToTryRate = eurToTryRate;
        project.UsdToTryRate = usdToTryRate;

        if (!TryValidateModel(project))
        {
            TempData["Error"] = "Bütçe veya kur bilgileri geçersiz.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await _context.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Bütçe ve kur bilgileri güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static void NormalizeCostItem(ProjectCostItem item)
    {
        item.ProjectId = item.ProjectId == Guid.Empty ? null : item.ProjectId;
        item.PurchaseOrderId = item.PurchaseOrderId == Guid.Empty ? null : item.PurchaseOrderId;
        item.Description = item.Description?.Trim() ?? string.Empty;
        item.Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim();
        item.Currency = CurrencyMetadata.NormalizeInput(item.Currency);

        if (!item.ProjectId.HasValue && !item.PurchaseOrderId.HasValue)
        {
            item.Type = CostItemType.Overhead;
        }
    }

    private async Task FillLookupsAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        ViewBag.Projects = await _context.Projects.AsNoTracking().ApplyRecordVisibility(User).OrderBy(x => x.Code).ToListAsync(cancellationToken);

        var ordersQuery = _context.PurchaseOrders
            .Include(x => x.Project)
            .AsNoTracking()
            .ApplyRecordVisibility(User);

        if (projectId.HasValue && projectId.Value != Guid.Empty)
        {
            ordersQuery = ordersQuery.Where(x => x.ProjectId == projectId.Value);
        }

        ViewBag.PurchaseOrders = await ordersQuery
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateCostRelationsAsync(ProjectCostItem item, CancellationToken cancellationToken)
    {
        if (item.ProjectId.HasValue)
        {
            var canUseProject = await _context.Projects
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == item.ProjectId.Value, cancellationToken);
            if (!canUseProject)
            {
                ModelState.AddModelError(nameof(item.ProjectId), "Seçilen proje için yetkiniz bulunmuyor.");
            }
        }

        if (item.PurchaseOrderId.HasValue)
        {
            var canUseOrder = await _context.PurchaseOrders
                .AsNoTracking()
                .ApplyRecordVisibility(User)
                .AnyAsync(x => x.Id == item.PurchaseOrderId.Value, cancellationToken);
            if (!canUseOrder)
            {
                ModelState.AddModelError(nameof(item.PurchaseOrderId), "Seçilen sipariş için yetkiniz bulunmuyor.");
            }
        }
    }

    private IQueryable<Project> BuildProjectsQuery(string? q, Guid? customerId, string? currency)
    {
        var query = _context.Projects
            .Include(x => x.Customer)
            .Include(x => x.CostItems)
            .Include(x => x.PurchaseOrders)
            .AsNoTracking()
            .ApplyRecordVisibility(User);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(term) ||
                x.Name.Contains(term) ||
                (x.Customer != null && x.Customer.Name.Contains(term)) ||
                (x.CustomerName != null && x.CustomerName.Contains(term)) ||
                (x.Description != null && x.Description.Contains(term)));
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalizedCurrency = CurrencyMetadata.NormalizeInput(currency, string.Empty);
            if (!string.IsNullOrWhiteSpace(normalizedCurrency))
            {
                query = query.Where(x =>
                    x.Currency == normalizedCurrency ||
                    x.CostItems.Any(cost => cost.Currency == normalizedCurrency) ||
                    x.PurchaseOrders.Any(order => order.Currency == normalizedCurrency));
            }
        }

        return query;
    }

    private IQueryable<Project> BuildProjectDetailsQuery()
    {
        return _context.Projects
            .Include(x => x.Customer)
            .Include(x => x.CostItems.OrderByDescending(cost => cost.CostDate ?? cost.CreatedAt))
                .ThenInclude(x => x.PurchaseOrder)
            .Include(x => x.PurchaseOrders.OrderByDescending(order => order.OrderDate ?? order.CreatedAt))
                .ThenInclude(x => x.Supplier)
            .Include(x => x.PurchaseOrders)
                .ThenInclude(x => x.Material)
            .AsNoTracking()
            .ApplyRecordVisibility(User);
    }

    private IQueryable<PurchaseOrder> BuildPurchaseOrdersQuery(string? q, Guid? projectId, Guid? supplierId, Guid? materialId, string? currency)
    {
        var query = _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking()
            .ApplyRecordVisibility(User);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.OrderNumber.Contains(term) ||
                x.Content.Contains(term) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))) ||
                (x.Supplier != null && x.Supplier.Name.Contains(term)) ||
                (x.Material != null && x.Material.Name.Contains(term)));
        }

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == supplierId.Value);
        }

        if (materialId.HasValue)
        {
            query = query.Where(x => x.MaterialId == materialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalizedCurrency = CurrencyMetadata.NormalizeInput(currency, string.Empty);
            if (!string.IsNullOrWhiteSpace(normalizedCurrency))
            {
                query = query.Where(x => x.Currency == normalizedCurrency);
            }
        }

        return query;
    }

    private IQueryable<PurchaseOrder> BuildMaterialOrdersQuery(string? q, string? currency)
    {
        var query = _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x => x.MaterialId != null);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                (x.Material != null && x.Material.Name.Contains(term)) ||
                x.Content.Contains(term) ||
                x.OrderNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalizedCurrency = CurrencyMetadata.NormalizeInput(currency, string.Empty);
            if (!string.IsNullOrWhiteSpace(normalizedCurrency))
            {
                query = query.Where(x => x.Currency == normalizedCurrency);
            }
        }

        return query;
    }

    private IQueryable<ProjectCostItem> BuildGeneralCostsQuery(string? q, Guid? projectId, string? currency)
    {
        var query = _context.ProjectCostItems
            .Include(x => x.Project)
            .Include(x => x.PurchaseOrder)
            .AsNoTracking()
            .ApplyRecordVisibility(User)
            .Where(x => x.Type == CostItemType.Overhead || x.Type == CostItemType.Other);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Description.Contains(term) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))) ||
                (x.Notes != null && x.Notes.Contains(term)));
        }

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalizedCurrency = CurrencyMetadata.NormalizeInput(currency, string.Empty);
            if (!string.IsNullOrWhiteSpace(normalizedCurrency))
            {
                query = query.Where(x => x.Currency == normalizedCurrency);
            }
        }

        return query;
    }

    private FileContentResult ExcelFile(IReadOnlyList<ExcelSheet> sheets, string fileName)
    {
        return File(
            ExcelWorkbookBuilder.Build(sheets),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private bool CanViewProjectBudget()
    {
        return User.IsInRole(AppRoles.Admin)
            || User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectBudgetView)
            || User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectBudgetManage);
    }

    private static string FormatCurrencyTotals(IEnumerable<CurrencyTotalViewModel> totals)
    {
        var values = totals
            .Select(x => $"{x.Amount:N2} {x.Currency}")
            .ToList();

        return values.Count == 0 ? "-" : string.Join(" | ", values);
    }
}
