using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class PurchaseOrderTemplateService : CrudService<PurchaseOrderTemplate>, IPurchaseOrderTemplateService
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderTemplateService(IRepository<PurchaseOrderTemplate> repository, ApplicationDbContext context) : base(repository)
    {
        _context = context;
    }

    protected override IQueryable<PurchaseOrderTemplate> ListQuery()
    {
        return Repository.Query()
            .Include(x => x.DefaultSupplier)
            .Include(x => x.Lines);
    }

    protected override IQueryable<PurchaseOrderTemplate> DetailsQuery()
    {
        return Repository.Query()
            .Include(x => x.DefaultSupplier)
            .Include(x => x.Lines)
                .ThenInclude(x => x.Supplier)
            .Include(x => x.Lines)
                .ThenInclude(x => x.Material);
    }

    public Task<PurchaseOrderTemplate?> GetTemplateWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DetailsQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<PurchaseOrderTemplateLine?> GetLineByIdAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default)
    {
        return _context.PurchaseOrderTemplateLines
            .FirstOrDefaultAsync(x => x.PurchaseOrderTemplateId == templateId && x.Id == lineId, cancellationToken);
    }

    public async Task AddLineAsync(PurchaseOrderTemplateLine line, CancellationToken cancellationToken = default)
    {
        line.SortOrder = await GetNextSortOrderAsync(line.PurchaseOrderTemplateId, cancellationToken);
        _context.PurchaseOrderTemplateLines.Add(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLineAsync(PurchaseOrderTemplateLine line, CancellationToken cancellationToken = default)
    {
        _context.PurchaseOrderTemplateLines.Update(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteLineAsync(Guid templateId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var line = await GetLineByIdAsync(templateId, lineId, cancellationToken);
        if (line is null)
        {
            return;
        }

        _context.PurchaseOrderTemplateLines.Remove(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ApplyTemplateAsync(Guid templateId, Guid? projectId, DateTime orderDate, string? requestedByUserId, string? requestedBy, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateWithLinesAsync(templateId, cancellationToken);
        if (template is null || !template.IsActive)
        {
            return 0;
        }

        if (projectId.HasValue)
        {
            var projectExists = await _context.Projects
                .AsNoTracking()
                .AnyAsync(x => x.Id == projectId.Value && x.Status != ProjectStatus.Cancelled, cancellationToken);

            if (!projectExists)
            {
                return 0;
            }
        }

        var validLines = template.Lines
            .OrderBy(x => x.SortOrder)
            .Where(x => !string.IsNullOrWhiteSpace(x.Content))
            .ToList();

        if (validLines.Count == 0)
        {
            return 0;
        }

        var createdOrders = new List<PurchaseOrder>();
        var reservedOrderNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in validLines)
        {
            var orderNumber = await GenerateOrderNumberAsync(cancellationToken, reservedOrderNumbers);
            reservedOrderNumbers.Add(orderNumber);

            var expectedArrivalDate = line.ExpectedArrivalOffsetDays.HasValue
                ? orderDate.Date.AddDays(line.ExpectedArrivalOffsetDays.Value)
                : (DateTime?)null;

            var supplierId = line.SupplierId ?? template.DefaultSupplierId;
            var scope = projectId.HasValue ? PurchaseOrderScope.Project : template.DefaultScope;
            var currency = string.IsNullOrWhiteSpace(template.DefaultCurrency) ? "TRY" : template.DefaultCurrency.Trim().ToUpperInvariant();
            var orderTotal = line.OrderTotal ?? (line.UnitPrice.HasValue && line.Quantity.HasValue ? line.UnitPrice.Value * line.Quantity.Value : null);

            var order = new PurchaseOrder
            {
                ProjectId = projectId,
                SupplierId = supplierId,
                MaterialId = line.MaterialId,
                OrderNumber = orderNumber,
                Scope = scope,
                TrackingState = 0,
                Content = line.Content.Trim(),
                Quantity = line.Quantity,
                QuantityText = string.IsNullOrWhiteSpace(line.QuantityText) && line.Quantity.HasValue
                    ? line.Quantity.Value.ToString("0.###")
                    : line.QuantityText?.Trim(),
                Unit = line.Unit?.Trim(),
                Quality = line.Quality?.Trim(),
                Status = template.DefaultStatus,
                OrderDate = orderDate.Date,
                ExpectedArrivalDate = expectedArrivalDate,
                RequestedBy = requestedBy,
                RequestedByUserId = requestedByUserId,
                PaymentTerm = template.DefaultPaymentTerm?.Trim(),
                UnitPrice = line.UnitPrice,
                UnitPriceText = line.UnitPrice.HasValue ? $"{line.UnitPrice.Value:N2} {currency}" : null,
                OrderTotal = orderTotal,
                Currency = currency,
                VatRate = template.DefaultVatRate,
                Notes = line.Notes?.Trim(),
                IsActive = true
            };

            _context.PurchaseOrders.Add(order);
            createdOrders.Add(order);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return createdOrders.Count;
    }

    private async Task<int> GetNextSortOrderAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrderTemplateLines
            .AsNoTracking()
            .Where(x => x.PurchaseOrderTemplateId == templateId);

        return await query.AnyAsync(cancellationToken)
            ? await query.MaxAsync(x => x.SortOrder, cancellationToken) + 1
            : 1;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken, ISet<string> reservedOrderNumbers)
    {
        var baseNumber = $"PO-{DateTime.Now:yyyyMMddHHmmss}";
        var orderNumber = baseNumber;
        var sequence = 1;

        while (reservedOrderNumbers.Contains(orderNumber) ||
               await _context.PurchaseOrders.AnyAsync(x => x.OrderNumber == orderNumber, cancellationToken))
        {
            orderNumber = $"{baseNumber}-{sequence:00}";
            sequence++;
        }

        return orderNumber;
    }
}
