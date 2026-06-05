using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewMaterials)]
public class MaterialsController : Controller
{
    private readonly IMaterialService _materialService;
    private readonly ApplicationDbContext _context;

    public MaterialsController(IMaterialService materialService, ApplicationDbContext context)
    {
        _materialService = materialService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? q, string? category, string? type, string? grade, string? sort, CancellationToken cancellationToken)
    {
        var query = _context.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Type != null && x.Type.Contains(term)) ||
                (x.Grade != null && x.Grade.Contains(term)) ||
                (x.Surface != null && x.Surface.Contains(term)) ||
                (x.Dimensions != null && x.Dimensions.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.CategoryName == category);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(x => x.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(grade))
        {
            query = query.Where(x => x.Grade == grade);
        }

        query = sort switch
        {
            "category" => query.OrderBy(x => x.CategoryName ?? x.Category.ToString()).ThenBy(x => x.Name),
            "type" => query.OrderBy(x => x.Type).ThenBy(x => x.Name),
            "grade" => query.OrderBy(x => x.Grade).ThenBy(x => x.Name),
            "created" => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderBy(x => x.Name)
        };

        ViewBag.FilterQ = q;
        ViewBag.FilterCategory = category;
        ViewBag.FilterType = type;
        ViewBag.FilterGrade = grade;
        ViewBag.Sort = sort;
        await FillFilterLookupsAsync(cancellationToken);

        return View(await query.ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken) => (await _materialService.GetDetailsAsync(id, cancellationToken)) is { } material ? View(material) : NotFound();

    [Authorize(Policy = AppPolicies.CanManageMaterials)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await FillCategoryLookupsAsync(cancellationToken);
        return View(new Material());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AppPolicies.CanManageMaterials)]
    public async Task<IActionResult> Create(Material material, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await FillCategoryLookupsAsync(cancellationToken);
            return View(material);
        }

        await _materialService.CreateAsync(material, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageMaterials)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var material = await _materialService.GetByIdAsync(id, cancellationToken);
        if (material is null)
        {
            return NotFound();
        }

        await FillCategoryLookupsAsync(cancellationToken);
        return View(material);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AppPolicies.CanManageMaterials)]
    public async Task<IActionResult> Edit(Guid id, Material material, CancellationToken cancellationToken)
    {
        if (id != material.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await FillCategoryLookupsAsync(cancellationToken);
            return View(material);
        }

        await _materialService.UpdateAsync(material, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageMaterials)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) => (await _materialService.GetDetailsAsync(id, cancellationToken)) is { } material ? View(material) : NotFound();

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Policy = AppPolicies.CanManageMaterials)]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await _materialService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageMaterials)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids.Distinct())
        {
            await _materialService.DeleteAsync(id, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task FillCategoryLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.MaterialCategories = await _context.MaterialCategoryDefinitions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task FillFilterLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.MaterialCategories = await _context.MaterialCategoryDefinitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        ViewBag.MaterialTypes = await _context.Materials.AsNoTracking().Where(x => x.Type != null && x.Type != string.Empty).Select(x => x.Type!).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        ViewBag.MaterialGrades = await _context.Materials.AsNoTracking().Where(x => x.Grade != null && x.Grade != string.Empty).Select(x => x.Grade!).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
    }
}
