using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Business.Web.Controllers;

[Authorize(Policy = AppPolicies.CanViewInvoices)]
public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILookupService _lookupService;
    private readonly ApplicationDbContext _context;

    public InvoicesController(IInvoiceService invoiceService, ILookupService lookupService, ApplicationDbContext context)
    {
        _invoiceService = invoiceService;
        _lookupService = lookupService;
        _context = context;
    }

    public async Task<IActionResult> Index(string? q, InvoiceType? type, InvoiceStatus? status, Guid? customerId, Guid? supplierId, Guid? projectId, DateTime? dateFrom, DateTime? dateTo, string? sort, CancellationToken cancellationToken)
    {
        var query = _context.Invoices
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .Include(x => x.Project)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.InvoiceNumber.Contains(term) ||
                (x.Notes != null && x.Notes.Contains(term)) ||
                (x.PaymentTerm != null && x.PaymentTerm.Contains(term)) ||
                (x.Customer != null && x.Customer.Name.Contains(term)) ||
                (x.Supplier != null && x.Supplier.Name.Contains(term)) ||
                (x.Project != null && (x.Project.Code.Contains(term) || x.Project.Name.Contains(term))));
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == supplierId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.IssueDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.IssueDate <= dateTo.Value);
        }

        query = sort switch
        {
            "number" => query.OrderBy(x => x.InvoiceNumber),
            "type" => query.OrderBy(x => x.Type),
            "status" => query.OrderBy(x => x.Status),
            "party" => query.OrderBy(x => x.Customer != null ? x.Customer.Name : x.Supplier != null ? x.Supplier.Name : string.Empty),
            "amount" => query.OrderByDescending(x => x.GrandTotal),
            "date" => query.OrderBy(x => x.IssueDate),
            "due" => query.OrderBy(x => x.DueDate ?? DateTime.MaxValue),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        ViewBag.FilterQ = q;
        ViewBag.FilterType = type;
        ViewBag.FilterStatus = status;
        ViewBag.FilterCustomerId = customerId;
        ViewBag.FilterSupplierId = supplierId;
        ViewBag.FilterProjectId = projectId;
        ViewBag.FilterDateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.FilterDateTo = dateTo?.ToString("yyyy-MM-dd");
        ViewBag.Sort = sort;
        await FillLookupsAsync(cancellationToken);

        return View(await query.ToListAsync(cancellationToken));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetDetailsAsync(id, cancellationToken);
        return invoice is null ? NotFound() : View(invoice);
    }

    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await FillLookupsAsync(cancellationToken);
        return View(new Invoice { InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmm}", IssueDate = DateTime.Today, Type = InvoiceType.Sales });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> Create(Invoice invoice, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(invoice);
        }

        invoice.GrandTotal = invoice.SubTotal + invoice.VatTotal - invoice.DiscountTotal;
        await _invoiceService.CreateAsync(invoice, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        return View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> Edit(Guid id, Invoice invoice, CancellationToken cancellationToken)
    {
        if (id != invoice.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(cancellationToken);
            return View(invoice);
        }

        invoice.GrandTotal = invoice.SubTotal + invoice.VatTotal - invoice.DiscountTotal;
        await _invoiceService.UpdateAsync(invoice, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetDetailsAsync(id, cancellationToken);
        return invoice is null ? NotFound() : View(invoice);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.Invoice && x.OwnerId == id));
        _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.Invoice && x.OwnerId == id));
        await _context.SaveChangesAsync(cancellationToken);
        await _invoiceService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Customers = await _lookupService.GetCustomersAsync(cancellationToken);
        ViewBag.Suppliers = await _lookupService.GetSuppliersAsync(cancellationToken);
        ViewBag.Projects = await _lookupService.GetProjectsAsync(cancellationToken);
        ViewBag.PurchaseOrders = await _lookupService.GetPurchaseOrdersAsync(cancellationToken);
    }
}
