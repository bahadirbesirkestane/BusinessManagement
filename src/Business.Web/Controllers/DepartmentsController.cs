using Business.Domain.Entities;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanManageUsers)]
public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _context.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken));
    }

    public IActionResult Create()
    {
        return View(new Department { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Department department, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(department);
        }

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var department = await _context.Departments.FindAsync([id], cancellationToken);
        return department is null ? NotFound() : View(department);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Department department, CancellationToken cancellationToken)
    {
        if (id != department.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(department);
        }

        _context.Update(department);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var department = await _context.Departments.FindAsync([id], cancellationToken);
        if (department is null)
        {
            return NotFound();
        }

        var hasUsers = await _context.Users.AnyAsync(x => x.DepartmentId == id, cancellationToken);
        if (hasUsers)
        {
            department.IsActive = false;
        }
        else
        {
            _context.Departments.Remove(department);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
