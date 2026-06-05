using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewStock)]
public class StockController : Controller
{
    private readonly ApplicationDbContext _context;

    public StockController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? q, string? category, StockStatus? status, string? sort, CancellationToken cancellationToken)
    {
        var query = BaseStockQuery()
            .Where(x => x.MaterialId != null && x.Material != null)
            .Where(x => x.Quantity == null || x.Quantity > 0 || x.QuantityText != null);

        if (!status.HasValue)
        {
            query = query.Where(x => x.Status != StockStatus.OutOfStock && x.Status != StockStatus.Scrapped);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Thickness != null && x.Thickness.Contains(term)) ||
                (x.Dimensions != null && x.Dimensions.Contains(term)) ||
                (x.Location != null && x.Location.Contains(term)) ||
                (x.Material != null && x.Material.Name.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Material != null && x.Material.CategoryName == category);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        query = sort switch
        {
            "material" => query.OrderBy(x => x.Material!.Name).ThenBy(x => x.Name),
            "type" => query.OrderBy(x => x.Name).ThenBy(x => x.Material!.Name),
            "quantity" => query.OrderByDescending(x => x.Quantity ?? 0),
            "status" => query.OrderBy(x => x.Status).ThenBy(x => x.Material!.Name),
            "location" => query.OrderBy(x => x.Location).ThenBy(x => x.Material!.Name),
            _ => query.OrderBy(x => x.Material!.Name).ThenBy(x => x.Name)
        };

        var items = await query.ToListAsync(cancellationToken);

        var model = items
            .GroupBy(x => x.MaterialId)
            .Select(group =>
            {
                var first = group.First();
                var units = group.Select(x => x.Unit).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                return new StockMaterialGroupViewModel
                {
                    Material = first.Material!,
                    Items = group.ToList(),
                    TotalQuantity = group.Where(x => x.Quantity.HasValue).Sum(x => x.Quantity!.Value),
                    UnitText = units.Count == 1 ? units[0]! : string.Empty,
                    LowStockCount = group.Count(x => x.Status == StockStatus.LowStock)
                };
            })
            .OrderBy(x => x.Material.Name)
            .ToList();

        ViewBag.FilterQ = q;
        ViewBag.FilterCategory = category;
        ViewBag.FilterStatus = status;
        ViewBag.Sort = sort;
        ViewBag.MaterialCategories = await _context.MaterialCategoryDefinitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id, string? q, StockStatus? status, string? sort, CancellationToken cancellationToken)
    {
        var material = await _context.Materials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (material is null)
        {
            return NotFound();
        }

        var query = BaseStockQuery()
            .Where(x => x.MaterialId == id);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Thickness != null && x.Thickness.Contains(term)) ||
                (x.Dimensions != null && x.Dimensions.Contains(term)) ||
                (x.QuantityText != null && x.QuantityText.Contains(term)) ||
                (x.Unit != null && x.Unit.Contains(term)) ||
                (x.Location != null && x.Location.Contains(term)) ||
                (x.Notes != null && x.Notes.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        query = sort switch
        {
            "type" => query.OrderBy(x => x.Name).ThenBy(x => x.Thickness).ThenBy(x => x.Dimensions),
            "thickness" => query.OrderBy(x => x.Thickness).ThenBy(x => x.Dimensions).ThenBy(x => x.Name),
            "dimensions" => query.OrderBy(x => x.Dimensions).ThenBy(x => x.Thickness).ThenBy(x => x.Name),
            "quantity" => query.OrderByDescending(x => x.Quantity ?? 0).ThenBy(x => x.Name),
            "status" => query.OrderBy(x => x.Status).ThenBy(x => x.Name),
            "location" => query.OrderBy(x => x.Location).ThenBy(x => x.Name),
            _ => query.OrderBy(x => x.Name).ThenBy(x => x.Thickness).ThenBy(x => x.Dimensions)
        };

        var items = await query.ToListAsync(cancellationToken);

        ViewBag.FilterQ = q;
        ViewBag.FilterStatus = status;
        ViewBag.Sort = sort;

        return View(new StockMaterialDetailsViewModel
        {
            Material = material,
            Groups = items.GroupBy(x => string.IsNullOrWhiteSpace(x.Name) ? material.Name : x.Name).ToList()
        });
    }

    public async Task<IActionResult> Critical(CancellationToken cancellationToken)
    {
        var items = await BaseStockQuery()
            .Where(x => x.Status == StockStatus.LowStock || x.Status == StockStatus.OutOfStock)
            .OrderBy(x => x.Material!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Kritik Stoklar";
        return View("Items", items);
    }

    public async Task<IActionResult> OutOfStock(CancellationToken cancellationToken)
    {
        var items = await BaseStockQuery()
            .Where(x => x.Status == StockStatus.OutOfStock)
            .OrderBy(x => x.Material!.Name)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = "Stok Çıkışları";
        return View("Items", items);
    }

    [Authorize(Policy = AppPolicies.CanManageStock)]
    public async Task<IActionResult> Create(Guid? materialId, CancellationToken cancellationToken)
    {
        await FillLookupsAsync(cancellationToken);
        return View(new StockItem { MaterialId = materialId, Status = StockStatus.InStock });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageStock)]
    public async Task<IActionResult> Create(StockItem item, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(item);
        }

        _context.StockItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item.MaterialId.HasValue
            ? RedirectToAction(nameof(Details), new { id = item.MaterialId.Value })
            : RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageStock)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.StockItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageStock)]
    public async Task<IActionResult> Edit(Guid id, StockItem item, CancellationToken cancellationToken)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(item);
        }

        _context.StockItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item.MaterialId.HasValue
            ? RedirectToAction(nameof(Details), new { id = item.MaterialId.Value })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageStock)]
    public async Task<IActionResult> Delete(Guid id, Guid? materialId, CancellationToken cancellationToken)
    {
        var item = await _context.StockItems.FindAsync([id], cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        _context.StockItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return materialId.HasValue
            ? RedirectToAction(nameof(Details), new { id = materialId.Value })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageStock)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, string? returnUrl, CancellationToken cancellationToken)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count > 0)
        {
            _context.StockItems.RemoveRange(_context.StockItems.Where(x => selectedIds.Contains(x.Id)));
            await _context.SaveChangesAsync(cancellationToken);
        }

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private IQueryable<StockItem> BaseStockQuery()
    {
        return _context.StockItems
            .Include(x => x.Material)
            .AsNoTracking();
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Materials = await _context.Materials.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }
}
