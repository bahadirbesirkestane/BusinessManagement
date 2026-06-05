using Business.Application.Services;
using Business.Domain.Entities;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewCustomers)]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _customerService.GetAllAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetDetailsAsync(id, cancellationToken);
        return customer is null ? NotFound() : View(customer);
    }

    [Authorize(Policy = AppPolicies.CanManageCustomers)]
    public IActionResult Create()
    {
        return View(new Customer());
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.CanManageCustomers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        await _customerService.CreateAsync(customer, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.CanManageCustomers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Customer customer, CancellationToken cancellationToken)
    {
        if (id != customer.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        await _customerService.UpdateAsync(customer, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetDetailsAsync(id, cancellationToken);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Policy = AppPolicies.CanManageCustomers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.CanManageCustomers)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(Guid[] ids, CancellationToken cancellationToken)
    {
        foreach (var id in ids.Distinct())
        {
            await _customerService.DeleteAsync(id, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }
}
