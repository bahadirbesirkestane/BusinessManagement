using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.Extensions;
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

    public async Task<IActionResult> Projects(CancellationToken cancellationToken)
    {
        var projects = await _context.Projects
            .Include(x => x.Customer)
            .Include(x => x.CostItems)
            .Include(x => x.PurchaseOrders)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(projects);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var project = await _context.Projects
            .Include(x => x.Customer)
            .Include(x => x.CostItems.OrderByDescending(cost => cost.CostDate ?? cost.CreatedAt))
                .ThenInclude(x => x.PurchaseOrder)
            .Include(x => x.PurchaseOrders.OrderByDescending(order => order.OrderDate ?? order.CreatedAt))
                .ThenInclude(x => x.Supplier)
            .Include(x => x.PurchaseOrders)
                .ThenInclude(x => x.Material)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return project is null ? NotFound() : View(project);
    }

    public async Task<IActionResult> PurchaseOrders(CancellationToken cancellationToken)
    {
        var orders = await _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    public async Task<IActionResult> Materials(CancellationToken cancellationToken)
    {
        var orders = await _context.PurchaseOrders
            .Include(x => x.Project)
            .Include(x => x.Supplier)
            .Include(x => x.Material)
            .AsNoTracking()
            .Where(x => x.MaterialId != null)
            .OrderBy(x => x.Material == null ? string.Empty : x.Material.Name)
            .ThenByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(orders);
    }

    public async Task<IActionResult> General(CancellationToken cancellationToken)
    {
        var items = await _context.ProjectCostItems
            .Include(x => x.Project)
            .Include(x => x.PurchaseOrder)
            .AsNoTracking()
            .Where(x => x.Type == CostItemType.Overhead || x.Type == CostItemType.Other)
            .OrderByDescending(x => x.CostDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        return View(items);
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
        var item = await _context.ProjectCostItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
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
        var item = await _context.ProjectCostItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
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
            _context.ProjectCostItems.RemoveRange(_context.ProjectCostItems.Where(x => selectedIds.Contains(x.Id)));
            await _context.SaveChangesAsync(cancellationToken);
        }

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(General));
    }

    private static void NormalizeCostItem(ProjectCostItem item)
    {
        item.ProjectId = item.ProjectId == Guid.Empty ? null : item.ProjectId;
        item.PurchaseOrderId = item.PurchaseOrderId == Guid.Empty ? null : item.PurchaseOrderId;

        if (!item.ProjectId.HasValue && !item.PurchaseOrderId.HasValue)
        {
            item.Type = CostItemType.Overhead;
        }
    }

    private async Task FillLookupsAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        ViewBag.Projects = await _context.Projects.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);

        var ordersQuery = _context.PurchaseOrders
            .Include(x => x.Project)
            .AsNoTracking();

        if (projectId.HasValue && projectId.Value != Guid.Empty)
        {
            ordersQuery = ordersQuery.Where(x => x.ProjectId == projectId.Value);
        }

        ViewBag.PurchaseOrders = await ordersQuery
            .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
