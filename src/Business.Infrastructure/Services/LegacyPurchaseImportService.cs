using Business.Application.Services;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Business.Infrastructure.Services;

public class LegacyPurchaseImportService : ILegacyPurchaseImportService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;
    private const string ProjectCodePrefix = "IN";
    private readonly ApplicationDbContext _context;

    public LegacyPurchaseImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LegacyPurchaseImportResult> ImportAsync(Stream workbookStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);

        var result = new LegacyPurchaseImportResult();

        using var workbook = new XLWorkbook(workbookStream);
        var projectsSheet = workbook.Worksheets.FirstOrDefault(x => x.Name.Equals("Projeler", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("'Projeler' sayfası bulunamadı.");
        var ordersSheet = workbook.Worksheets.FirstOrDefault(x => x.Name.Equals("Siparisler", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("'Siparisler' sayfası bulunamadı.");

        var projectRows = ReadRows(projectsSheet);
        var orderRows = ReadRows(ordersSheet);

        var projectCodes = projectRows
            .Select(x => NormalizeProjectCode(x.GetValueOrDefault("ProjectCode")))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingProjects = await _context.Projects
            .Where(x => projectCodes.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var supplierLookup = await _context.Suppliers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var suppliersByNormalizedName = supplierLookup
            .GroupBy(x => NormalizeText(x.Name), NameComparer)
            .ToDictionary(x => x.Key, x => x.First(), NameComparer);

        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var row in projectRows)
            {
                var projectCode = NormalizeProjectCode(row.GetValueOrDefault("ProjectCode"));
                if (string.IsNullOrWhiteSpace(projectCode))
                {
                    continue;
                }

                if (existingProjects.ContainsKey(projectCode))
                {
                    result.ReusedProjects++;
                    continue;
                }

                var project = new Project
                {
                    Code = projectCode,
                    Name = row.GetValueOrDefault("ProjectName")?.Trim() ?? projectCode,
                    Status = ParseProjectStatus(row.GetValueOrDefault("ProjectStatus")),
                    Visibility = RecordVisibility.General
                };

                _context.Projects.Add(project);
                existingProjects[projectCode] = project;
                result.CreatedProjects++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            var sourceKeys = orderRows
                .Select(CreateSourceOrderNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingOrders = await _context.PurchaseOrders
                .Where(x => sourceKeys.Contains(x.OrderNumber))
                .ToDictionaryAsync(x => x.OrderNumber, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var updatedOrders = 0;

            foreach (var row in orderRows)
            {
                var orderNumber = CreateSourceOrderNumber(row);
                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    result.SkippedOrders++;
                    result.Messages.Add($"Sipariş satırı atlandı: kaynak numarası üretilemedi ({DescribeRow(row)}).");
                    continue;
                }

                var content = row.GetValueOrDefault("Content")?.Trim();
                if (string.IsNullOrWhiteSpace(content))
                {
                    result.SkippedOrders++;
                    result.Messages.Add($"Sipariş satırı atlandı: içerik boş ({DescribeRow(row)}).");
                    continue;
                }

                var scope = ParseScope(row.GetValueOrDefault("Scope"));
                Guid? projectId = null;
                var projectCode = NormalizeProjectCode(row.GetValueOrDefault("ProjectCode"));
                if (scope == PurchaseOrderScope.Project)
                {
                    if (string.IsNullOrWhiteSpace(projectCode) || !existingProjects.TryGetValue(projectCode, out var project))
                    {
                        result.SkippedOrders++;
                        result.Messages.Add($"Sipariş satırı atlandı: proje bulunamadı ({DescribeRow(row)}).");
                        continue;
                    }

                    projectId = project.Id;
                }

                Guid? supplierId = null;
                var supplierName = row.GetValueOrDefault("SupplierName")?.Trim();
                if (!string.IsNullOrWhiteSpace(supplierName))
                {
                    var normalizedSupplierName = NormalizeText(supplierName);
                    if (suppliersByNormalizedName.TryGetValue(normalizedSupplierName, out var supplier))
                    {
                        supplierId = supplier.Id;
                        result.MatchedSuppliers++;
                    }
                    else
                    {
                        result.UnmatchedSuppliers++;
                    }
                }

                var quantity = ParseNullableDecimal(row.GetValueOrDefault("Quantity"));
                var unitPrice = ParseNullableDecimal(row.GetValueOrDefault("UnitPrice"));
                var currency = NormalizeCurrency(row.GetValueOrDefault("Currency"), unitPrice);
                var orderTotal = ParseNullableDecimal(row.GetValueOrDefault("OrderTotal"))
                    ?? CalculateOrderTotal(quantity, unitPrice);

                if (existingOrders.TryGetValue(orderNumber, out var existingOrder))
                {
                    ApplyImportedValues(
                        existingOrder,
                        projectId,
                        supplierId,
                        scope,
                        content,
                        quantity,
                        row.GetValueOrDefault("QuantityText"),
                        row.GetValueOrDefault("Unit"),
                        row.GetValueOrDefault("Quality"),
                        row.GetValueOrDefault("OrderStatus"),
                        row.GetValueOrDefault("OrderDate"),
                        row.GetValueOrDefault("ArrivalDate"),
                        row.GetValueOrDefault("RequestedBy"),
                        unitPrice,
                        row.GetValueOrDefault("UnitPriceText"),
                        orderTotal,
                        currency,
                        row.GetValueOrDefault("Notes"));
                    updatedOrders++;
                    continue;
                }

                var order = new PurchaseOrder
                {
                    OrderNumber = orderNumber
                };

                ApplyImportedValues(
                    order,
                    projectId,
                    supplierId,
                    scope,
                    content,
                    quantity,
                    row.GetValueOrDefault("QuantityText"),
                    row.GetValueOrDefault("Unit"),
                    row.GetValueOrDefault("Quality"),
                    row.GetValueOrDefault("OrderStatus"),
                    row.GetValueOrDefault("OrderDate"),
                    row.GetValueOrDefault("ArrivalDate"),
                    row.GetValueOrDefault("RequestedBy"),
                    unitPrice,
                    row.GetValueOrDefault("UnitPriceText"),
                    orderTotal,
                    currency,
                    row.GetValueOrDefault("Notes"));

                _context.PurchaseOrders.Add(order);
                existingOrders[orderNumber] = order;
                result.ImportedOrders++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (updatedOrders > 0)
            {
                result.Messages.Add($"{updatedOrders} mevcut sipariş kaydı yeni Excel verisiyle güncellendi.");
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (result.UnmatchedSuppliers > 0)
        {
            result.Messages.Add($"{result.UnmatchedSuppliers} siparişte tedarikçi eşleşmesi bulunamadı; siparişler tedarikçisiz içeri alındı.");
        }

        return result;
    }

    private static List<Dictionary<string, string?>> ReadRows(IXLWorksheet sheet)
    {
        var usedRange = sheet.RangeUsed() ?? throw new InvalidOperationException($"'{sheet.Name}' sayfasında veri bulunamadı.");
        var headerRow = usedRange.FirstRow();
        var headerMap = headerRow.Cells()
            .Select((cell, index) => new { Index = index + 1, Header = cell.GetString().Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Header))
            .ToDictionary(x => x.Index, x => x.Header, EqualityComparer<int>.Default);

        return usedRange.RowsUsed().Skip(1)
            .Select(row =>
            {
                var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (index, header) in headerMap)
                {
                    values[header] = ReadCellValue(row.Cell(index));
                }

                return values;
            })
            .Where(x => x.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
            .ToList();
    }

    private static string? ReadCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        try
        {
            if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dateValue))
            {
                return dateValue.ToString("yyyy-MM-dd");
            }
        }
        catch (ArgumentException exception) when (exception.Message.Contains("Serial date 60", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number && cell.TryGetValue<decimal>(out var decimalValue))
        {
            return decimalValue.ToString("0.####", CultureInfo.InvariantCulture);
        }

        return cell.GetString().Trim();
    }

    private static ProjectStatus ParseProjectStatus(string? status)
    {
        return NormalizeText(status) switch
        {
            "COMPLETED" => ProjectStatus.Completed,
            "WAITING" => ProjectStatus.Waiting,
            "CANCELLED" => ProjectStatus.Cancelled,
            "INPROGRESS" => ProjectStatus.InProgress,
            "ACTIVE" => ProjectStatus.InProgress,
            "PLANNED" => ProjectStatus.Planned,
            _ => ProjectStatus.InProgress
        };
    }

    private static PurchaseOrderStatus ParseOrderStatus(string? status)
    {
        return NormalizeText(status) switch
        {
            "DELIVERED" => PurchaseOrderStatus.Delivered,
            "CANCELLED" => PurchaseOrderStatus.Cancelled,
            "ORDERED" => PurchaseOrderStatus.Ordered,
            "PARTIALLYDELIVERED" => PurchaseOrderStatus.PartiallyDelivered,
            "DRAFT" => PurchaseOrderStatus.Draft,
            _ => PurchaseOrderStatus.Requested
        };
    }

    private static PurchaseOrderScope ParseScope(string? scope)
    {
        return NormalizeText(scope) == "PROJECT"
            ? PurchaseOrderScope.Project
            : PurchaseOrderScope.General;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NormalizeDecimalText(value);
        if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantResult))
        {
            return invariantResult;
        }

        return null;
    }

    private static DateTime? ParseNullableDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var parsed)
            ? parsed.Date
            : null;
    }

    private static string NormalizeCurrency(string? value, decimal? unitPrice)
    {
        var normalized = NormalizeText(value);
        if (normalized is "EUR" or "USD" or "TRY")
        {
            return normalized;
        }

        return unitPrice.HasValue ? "TRY" : "TRY";
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FormatQuantityText(decimal? quantity, string? unit)
    {
        if (!quantity.HasValue)
        {
            return null;
        }

        var numberText = quantity.Value.ToString("0.####", CultureInfo.InvariantCulture);
        var trimmedUnit = NullIfWhiteSpace(unit);
        return trimmedUnit is null ? numberText : $"{numberText} {trimmedUnit}";
    }

    private static string? FormatPriceText(decimal? unitPrice, string currency)
    {
        if (!unitPrice.HasValue)
        {
            return null;
        }

        var priceText = unitPrice.Value.ToString("0.####", CultureInfo.InvariantCulture);
        return $"{priceText} {currency}";
    }

    private static decimal? CalculateOrderTotal(decimal? quantity, decimal? unitPrice)
    {
        if (!quantity.HasValue || !unitPrice.HasValue)
        {
            return null;
        }

        return quantity.Value * unitPrice.Value;
    }

    private static void ApplyImportedValues(
        PurchaseOrder order,
        Guid? projectId,
        Guid? supplierId,
        PurchaseOrderScope scope,
        string content,
        decimal? quantity,
        string? quantityText,
        string? unit,
        string? quality,
        string? orderStatus,
        string? orderDate,
        string? arrivalDate,
        string? requestedBy,
        decimal? unitPrice,
        string? unitPriceText,
        decimal? orderTotal,
        string currency,
        string? notes)
    {
        order.ProjectId = projectId;
        order.SupplierId = supplierId;
        order.Visibility = RecordVisibility.General;
        order.Scope = scope;
        order.Content = content;
        order.Quantity = quantity;
        order.QuantityText = NullIfWhiteSpace(quantityText) ?? FormatQuantityText(quantity, unit);
        order.Unit = NullIfWhiteSpace(unit);
        order.Quality = NullIfWhiteSpace(quality);
        order.Status = ParseOrderStatus(orderStatus);
        order.OrderDate = ParseNullableDate(orderDate);
        order.ArrivalDate = ParseNullableDate(arrivalDate);
        order.RequestedBy = NullIfWhiteSpace(requestedBy);
        order.UnitPrice = unitPrice;
        order.UnitPriceText = NullIfWhiteSpace(unitPriceText) ?? FormatPriceText(unitPrice, currency);
        order.OrderTotal = orderTotal;
        order.Currency = currency;
        order.Notes = NullIfWhiteSpace(notes);
        order.IsActive = true;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static string NormalizeDecimalText(string value)
    {
        var cleaned = value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        var hasDot = cleaned.Contains('.');
        var hasComma = cleaned.Contains(',');

        if (hasDot && hasComma)
        {
            return cleaned.LastIndexOf(',') > cleaned.LastIndexOf('.')
                ? cleaned.Replace(".", string.Empty, StringComparison.Ordinal).Replace(",", ".", StringComparison.Ordinal)
                : cleaned.Replace(",", string.Empty, StringComparison.Ordinal);
        }

        if (hasDot)
        {
            var parts = cleaned.Split('.');
            if (parts.Length > 2 && parts.All(static part => part.All(char.IsDigit)))
            {
                return string.Concat(parts);
            }

            if (parts.Length == 2 && parts[0].All(char.IsDigit) && parts[1].All(char.IsDigit) && parts[1].Length == 3)
            {
                return cleaned.Replace(".", string.Empty, StringComparison.Ordinal);
            }

            return cleaned;
        }

        if (hasComma)
        {
            var parts = cleaned.Split(',');
            if (parts.Length > 2 && parts.All(static part => part.All(char.IsDigit)))
            {
                return string.Concat(parts);
            }

            if (parts.Length == 2 && parts[0].Length > 1 && parts[0].All(char.IsDigit) && parts[1].All(char.IsDigit) && parts[1].Length == 3)
            {
                return cleaned.Replace(",", string.Empty, StringComparison.Ordinal);
            }

            return cleaned.Replace(",", ".", StringComparison.Ordinal);
        }

        return cleaned;
    }

    private static string? CreateSourceOrderNumber(IReadOnlyDictionary<string, string?> row)
    {
        var scope = ParseScope(row.GetValueOrDefault("Scope"));
        var projectCode = NormalizeProjectCode(row.GetValueOrDefault("ProjectCode"));
        var sourceRow = row.GetValueOrDefault("SourceRow")?.Trim();

        if (string.IsNullOrWhiteSpace(sourceRow))
        {
            return null;
        }

        var scopeKey = scope == PurchaseOrderScope.Project && !string.IsNullOrWhiteSpace(projectCode)
            ? projectCode.Replace(" ", string.Empty, StringComparison.Ordinal)
            : "GEN";

        return $"IMP-{scopeKey}-{sourceRow}";
    }

    private static string? NormalizeProjectCode(string? projectCode)
    {
        if (string.IsNullOrWhiteSpace(projectCode))
        {
            return null;
        }

        var trimmed = projectCode.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        if (trimmed.StartsWith(ProjectCodePrefix, StringComparison.Ordinal))
        {
            return trimmed;
        }

        return $"{ProjectCodePrefix}{trimmed}";
    }

    private static string DescribeRow(IReadOnlyDictionary<string, string?> row)
    {
        var sourceSheet = row.GetValueOrDefault("SourceSheet") ?? "?";
        var sourceRow = row.GetValueOrDefault("SourceRow") ?? "?";
        return $"{sourceSheet}/{sourceRow}";
    }
}
