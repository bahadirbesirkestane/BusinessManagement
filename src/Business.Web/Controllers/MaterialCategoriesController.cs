using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanManageMaterials)]
public class MaterialCategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public MaterialCategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await _context.MaterialCategoryDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return View(categories);
    }

    public IActionResult Create()
    {
        return View(new MaterialCategoryDefinition { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaterialCategoryDefinition category, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        _context.MaterialCategoryDefinitions.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var category = await _context.MaterialCategoryDefinitions.FindAsync([id], cancellationToken);
        return category is null ? NotFound() : View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MaterialCategoryDefinition category, CancellationToken cancellationToken)
    {
        if (id != category.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(category);
        }

        var oldName = await _context.MaterialCategoryDefinitions
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        _context.MaterialCategoryDefinitions.Update(category);
        if (!string.IsNullOrWhiteSpace(oldName) && !oldName.Equals(category.Name, StringComparison.OrdinalIgnoreCase))
        {
            await _context.Materials
                .Where(x => x.CategoryName == oldName)
                .ExecuteUpdateAsync(x => x.SetProperty(material => material.CategoryName, category.Name), cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var category = await _context.MaterialCategoryDefinitions.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var hasMaterials = await _context.Materials.AnyAsync(x => x.CategoryName == category.Name, cancellationToken);
        if (hasMaterials)
        {
            category.IsActive = false;
        }
        else
        {
            _context.MaterialCategoryDefinitions.Remove(category);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
