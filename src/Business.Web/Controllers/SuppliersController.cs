using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewSuppliers)]
public class SuppliersController : Controller
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await _supplierService.GetAllAsync(cancellationToken));
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken) => (await _supplierService.GetDetailsAsync(id, cancellationToken)) is { } supplier ? View(supplier) : NotFound();

    [Authorize(Policy = AppPolicies.CanManageSuppliers)]
    public IActionResult Create() => View(new Supplier());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AppPolicies.CanManageSuppliers)]
    public async Task<IActionResult> Create(Supplier supplier, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(supplier);
        await _supplierService.CreateAsync(supplier, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageSuppliers)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken) => (await _supplierService.GetByIdAsync(id, cancellationToken)) is { } supplier ? View(supplier) : NotFound();

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AppPolicies.CanManageSuppliers)]
    public async Task<IActionResult> Edit(Guid id, Supplier supplier, CancellationToken cancellationToken)
    {
        if (id != supplier.Id) return BadRequest();
        if (!ModelState.IsValid) return View(supplier);
        await _supplierService.UpdateAsync(supplier, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageSuppliers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) => (await _supplierService.GetDetailsAsync(id, cancellationToken)) is { } supplier ? View(supplier) : NotFound();

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Policy = AppPolicies.CanManageSuppliers)]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await _supplierService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageSuppliers)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids.Distinct())
        {
            await _supplierService.DeleteAsync(id, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }
}
