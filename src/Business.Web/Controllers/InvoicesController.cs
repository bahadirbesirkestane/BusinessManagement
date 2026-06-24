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
        var invoice = new Invoice { InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmm}", IssueDate = DateTime.Today, Type = InvoiceType.Sales };
        EnsureInvoiceRows(invoice);
        return View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> Create(Invoice invoice, CancellationToken cancellationToken)
    {
        var lines = PrepareInvoiceLines(invoice);
        var persistedLines = CreatePersistedInvoiceLines(lines);
        ClearInvoiceLineModelState();
        ValidateInvoiceLines(lines);
        if (lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Kaydedilecek en az bir fatura satırı girin.");
        }

        if (!ModelState.IsValid)
        {
            EnsureInvoiceRows(invoice);
            await FillLookupsAsync(cancellationToken);
            return View(invoice);
        }

        invoice.Lines = persistedLines;
        RecalculateInvoiceTotals(invoice, persistedLines);
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        await FillLookupsAsync(cancellationToken);
        EnsureInvoiceRows(invoice);
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

        var lines = PrepareInvoiceLines(invoice);
        var persistedLines = CreatePersistedInvoiceLines(lines);
        ClearInvoiceLineModelState();
        ValidateInvoiceLines(lines);
        if (lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Kaydedilecek en az bir fatura satırı girin.");
        }

        if (!ModelState.IsValid)
        {
            EnsureInvoiceRows(invoice);
            await FillLookupsAsync(cancellationToken);
            return View(invoice);
        }

        var existingInvoice = await _context.Invoices
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existingInvoice is null)
        {
            return NotFound();
        }

        existingInvoice.CustomerId = invoice.CustomerId;
        existingInvoice.SupplierId = invoice.SupplierId;
        existingInvoice.ProjectId = invoice.ProjectId;
        existingInvoice.PurchaseOrderId = invoice.PurchaseOrderId;
        existingInvoice.InvoiceNumber = invoice.InvoiceNumber;
        existingInvoice.Type = invoice.Type;
        existingInvoice.Status = invoice.Status;
        existingInvoice.IssueDate = invoice.IssueDate;
        existingInvoice.DueDate = invoice.DueDate;
        existingInvoice.PaidAt = invoice.PaidAt;
        existingInvoice.Currency = invoice.Currency;
        existingInvoice.PaymentTerm = invoice.PaymentTerm;
        existingInvoice.Notes = invoice.Notes;

        RecalculateInvoiceTotals(existingInvoice, persistedLines);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _context.InvoiceLines
            .Where(x => x.InvoiceId == existingInvoice.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var line in persistedLines)
        {
            line.InvoiceId = existingInvoice.Id;
        }

        _context.InvoiceLines.AddRange(persistedLines);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageInvoices)]
    public async Task<IActionResult> BulkDelete(Guid[] ids, CancellationToken cancellationToken)
    {
        var selectedIds = ids.Distinct().ToList();
        if (selectedIds.Count > 0)
        {
            _context.RecordComments.RemoveRange(_context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.Invoice && selectedIds.Contains(x.OwnerId)));
            _context.RecordFiles.RemoveRange(_context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.Invoice && selectedIds.Contains(x.OwnerId)));
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var id in selectedIds)
            {
                await _invoiceService.DeleteAsync(id, cancellationToken);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task FillLookupsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Customers = await _lookupService.GetCustomersAsync(cancellationToken);
        ViewBag.Suppliers = await _lookupService.GetSuppliersAsync(cancellationToken);
        ViewBag.Projects = await _lookupService.GetProjectsAsync(cancellationToken);
        ViewBag.PurchaseOrders = await _lookupService.GetPurchaseOrdersAsync(cancellationToken);
        ViewBag.Materials = await _lookupService.GetMaterialsAsync(cancellationToken);
    }

    private static void EnsureInvoiceRows(Invoice invoice)
    {
        while (invoice.Lines.Count < 2)
        {
            invoice.Lines.Add(new InvoiceLine { Quantity = 1, VatRate = 20 });
        }
    }

    private static List<InvoiceLine> PrepareInvoiceLines(Invoice invoice)
    {
        var lines = invoice.Lines
            .Where(x => !string.IsNullOrWhiteSpace(x.Description))
            .ToList();

        foreach (var line in lines)
        {
            line.Description = line.Description.Trim();
            line.Unit = line.Unit?.Trim();
            line.Notes = line.Notes?.Trim();
            line.LineTotal = CalculateLineTotal(line, out _);
        }

        invoice.Lines = lines;
        return lines;
    }

    private static List<InvoiceLine> CreatePersistedInvoiceLines(IEnumerable<InvoiceLine> lines)
    {
        return lines
            .Select(line => new InvoiceLine
            {
                Description = line.Description,
                MaterialId = line.MaterialId,
                Quantity = line.Quantity,
                Unit = line.Unit,
                UnitPrice = line.UnitPrice,
                VatRate = line.VatRate,
                DiscountAmount = line.DiscountAmount,
                LineTotal = line.LineTotal,
                Notes = line.Notes
            })
            .ToList();
    }

    private void ClearInvoiceLineModelState()
    {
        foreach (var key in ModelState.Keys.Where(x => x.StartsWith("Lines[", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            ModelState.Remove(key);
        }
    }

    private void ValidateInvoiceLines(IEnumerable<InvoiceLine> lines)
    {
        foreach (var line in lines)
        {
            if (line.Quantity < 0)
            {
                ModelState.AddModelError(string.Empty, "Fatura satırı miktarı 0'dan küçük olamaz.");
            }

            if (line.UnitPrice < 0)
            {
                ModelState.AddModelError(string.Empty, "Fatura satırı birim fiyatı 0'dan küçük olamaz.");
            }

            if (line.VatRate < 0 || line.VatRate > 100)
            {
                ModelState.AddModelError(string.Empty, "Fatura satırı KDV oranı 0 ile 100 arasında olmalıdır.");
            }

            if (line.DiscountAmount < 0)
            {
                ModelState.AddModelError(string.Empty, "Fatura satırı iskontosu 0'dan küçük olamaz.");
            }
        }
    }

    private static void RecalculateInvoiceTotals(Invoice invoice)
    {
        RecalculateInvoiceTotals(invoice, invoice.Lines);
    }

    private static void RecalculateInvoiceTotals(Invoice invoice, IEnumerable<InvoiceLine> lines)
    {
        invoice.Currency = string.IsNullOrWhiteSpace(invoice.Currency)
            ? "TRY"
            : invoice.Currency.Trim().ToUpperInvariant();
        invoice.PaymentTerm = invoice.PaymentTerm?.Trim();
        invoice.Notes = invoice.Notes?.Trim();

        var subtotal = 0m;
        var vatTotal = 0m;
        var discountTotal = 0m;
        var grandTotal = 0m;

        foreach (var line in lines)
        {
            var lineTotal = CalculateLineTotal(line, out var vatAmount);
            line.LineTotal = lineTotal;
            subtotal += line.Quantity * line.UnitPrice;
            vatTotal += vatAmount;
            discountTotal += line.DiscountAmount;
            grandTotal += lineTotal;
        }

        invoice.SubTotal = subtotal;
        invoice.VatTotal = vatTotal;
        invoice.DiscountTotal = discountTotal;
        invoice.GrandTotal = grandTotal;
    }

    private static decimal CalculateLineTotal(InvoiceLine line, out decimal vatAmount)
    {
        var subtotal = line.Quantity * line.UnitPrice;
        vatAmount = subtotal * line.VatRate / 100m;
        return subtotal + vatAmount - line.DiscountAmount;
    }
}
