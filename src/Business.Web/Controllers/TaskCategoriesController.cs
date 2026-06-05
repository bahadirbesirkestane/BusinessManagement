using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanManageProjects)]
public class TaskCategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public TaskCategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await _context.TaskCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return View(categories);
    }

    public IActionResult Create()
    {
        return View(new TaskCategory { Color = "#2563eb", IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskCategory category, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        _context.TaskCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var category = await _context.TaskCategories.FindAsync([id], cancellationToken);
        return category is null ? NotFound() : View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TaskCategory category, CancellationToken cancellationToken)
    {
        if (id != category.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(category);
        }

        _context.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var category = await _context.TaskCategories.FindAsync([id], cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var hasTasks = await _context.ProjectTasks.AnyAsync(x => x.TaskCategoryId == id, cancellationToken);
        if (hasTasks)
        {
            category.IsActive = false;
        }
        else
        {
            _context.TaskCategories.Remove(category);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
