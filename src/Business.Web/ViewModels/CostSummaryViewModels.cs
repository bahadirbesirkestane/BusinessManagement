using Business.Domain.Entities;
using Business.Web.Extensions;

namespace Business.Web.ViewModels;

public class CurrencyTotalViewModel
{
    public string Currency { get; init; } = CurrencyMetadata.Try;
    public decimal Amount { get; init; }
}

public class ProjectCostSummaryViewModel
{
    public string ProjectCurrency { get; init; } = CurrencyMetadata.Try;
    public decimal? Budget { get; init; }
    public decimal? EurToTryRate { get; init; }
    public decimal? UsdToTryRate { get; init; }
    public decimal? CostItemsTotalInProjectCurrency { get; init; }
    public decimal? PurchaseOrdersTotalInProjectCurrency { get; init; }
    public decimal? GrandTotalInProjectCurrency { get; init; }
    public decimal? RemainingBudgetInProjectCurrency { get; init; }
    public bool HasMissingExchangeRates { get; init; }
    public IReadOnlyList<string> MissingRateCurrencies { get; init; } = [];
    public IReadOnlyList<CurrencyTotalViewModel> CostItemTotals { get; init; } = [];
    public IReadOnlyList<CurrencyTotalViewModel> PurchaseOrderTotals { get; init; } = [];
    public IReadOnlyList<CurrencyTotalViewModel> CombinedTotals { get; init; } = [];
}

public static class CostSummaryBuilder
{
    public static ProjectCostSummaryViewModel Build(Project project)
    {
        var projectCurrency = CurrencyMetadata.NormalizeStored(project.Currency);
        var costItemEntries = project.CostItems
            .Select(item => (Currency: CurrencyMetadata.NormalizeStored(item.Currency), Amount: item.Amount))
            .ToList();
        var purchaseOrderEntries = project.PurchaseOrders
            .Select(order => (Currency: CurrencyMetadata.NormalizeStored(order.Currency), Amount: GetOrderAmount(order)))
            .ToList();
        var combinedEntries = costItemEntries.Concat(purchaseOrderEntries).ToList();

        var missingCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var costItemsTotal = TryConvertEntries(costItemEntries, projectCurrency, project.EurToTryRate, project.UsdToTryRate, missingCurrencies);
        var purchaseOrdersTotal = TryConvertEntries(purchaseOrderEntries, projectCurrency, project.EurToTryRate, project.UsdToTryRate, missingCurrencies);
        var grandTotal = TryConvertEntries(combinedEntries, projectCurrency, project.EurToTryRate, project.UsdToTryRate, missingCurrencies);

        return new ProjectCostSummaryViewModel
        {
            ProjectCurrency = projectCurrency,
            Budget = project.Budget,
            EurToTryRate = project.EurToTryRate,
            UsdToTryRate = project.UsdToTryRate,
            CostItemsTotalInProjectCurrency = costItemsTotal,
            PurchaseOrdersTotalInProjectCurrency = purchaseOrdersTotal,
            GrandTotalInProjectCurrency = grandTotal,
            RemainingBudgetInProjectCurrency = project.Budget.HasValue && grandTotal.HasValue
                ? project.Budget.Value - grandTotal.Value
                : null,
            HasMissingExchangeRates = missingCurrencies.Count > 0,
            MissingRateCurrencies = missingCurrencies.OrderBy(x => x).ToList(),
            CostItemTotals = GroupByCurrency(costItemEntries),
            PurchaseOrderTotals = GroupByCurrency(purchaseOrderEntries),
            CombinedTotals = GroupByCurrency(combinedEntries)
        };
    }

    public static IReadOnlyList<CurrencyTotalViewModel> GroupByCurrency(IEnumerable<(string Currency, decimal Amount)> entries)
    {
        return entries
            .GroupBy(x => CurrencyMetadata.NormalizeStored(x.Currency))
            .OrderBy(x => x.Key)
            .Select(x => new CurrencyTotalViewModel
            {
                Currency = x.Key,
                Amount = x.Sum(item => item.Amount)
            })
            .ToList();
    }

    public static decimal GetOrderAmount(PurchaseOrder order)
    {
        return order.OrderTotal ?? ((order.UnitPrice ?? 0) * (order.Quantity ?? 0));
    }

    private static decimal? TryConvertEntries(
        IEnumerable<(string Currency, decimal Amount)> entries,
        string targetCurrency,
        decimal? eurToTryRate,
        decimal? usdToTryRate,
        ISet<string> missingCurrencies)
    {
        decimal total = 0;

        foreach (var entry in entries)
        {
            if (!TryConvert(entry.Amount, entry.Currency, targetCurrency, eurToTryRate, usdToTryRate, out var convertedAmount))
            {
                missingCurrencies.Add(CurrencyMetadata.NormalizeStored(entry.Currency));
                return null;
            }

            total += convertedAmount;
        }

        return total;
    }

    private static bool TryConvert(
        decimal amount,
        string sourceCurrency,
        string targetCurrency,
        decimal? eurToTryRate,
        decimal? usdToTryRate,
        out decimal convertedAmount)
    {
        sourceCurrency = CurrencyMetadata.NormalizeStored(sourceCurrency);
        targetCurrency = CurrencyMetadata.NormalizeStored(targetCurrency);

        if (sourceCurrency == targetCurrency)
        {
            convertedAmount = amount;
            return true;
        }

        if (!TryConvertToTry(amount, sourceCurrency, eurToTryRate, usdToTryRate, out var tryAmount))
        {
            convertedAmount = 0;
            return false;
        }

        if (!TryConvertFromTry(tryAmount, targetCurrency, eurToTryRate, usdToTryRate, out convertedAmount))
        {
            convertedAmount = 0;
            return false;
        }

        return true;
    }

    private static bool TryConvertToTry(decimal amount, string sourceCurrency, decimal? eurToTryRate, decimal? usdToTryRate, out decimal tryAmount)
    {
        switch (CurrencyMetadata.NormalizeStored(sourceCurrency))
        {
            case CurrencyMetadata.Try:
                tryAmount = amount;
                return true;
            case CurrencyMetadata.Eur when eurToTryRate.HasValue && eurToTryRate.Value > 0:
                tryAmount = amount * eurToTryRate.Value;
                return true;
            case CurrencyMetadata.Usd when usdToTryRate.HasValue && usdToTryRate.Value > 0:
                tryAmount = amount * usdToTryRate.Value;
                return true;
            default:
                tryAmount = 0;
                return false;
        }
    }

    private static bool TryConvertFromTry(decimal tryAmount, string targetCurrency, decimal? eurToTryRate, decimal? usdToTryRate, out decimal convertedAmount)
    {
        switch (CurrencyMetadata.NormalizeStored(targetCurrency))
        {
            case CurrencyMetadata.Try:
                convertedAmount = tryAmount;
                return true;
            case CurrencyMetadata.Eur when eurToTryRate.HasValue && eurToTryRate.Value > 0:
                convertedAmount = tryAmount / eurToTryRate.Value;
                return true;
            case CurrencyMetadata.Usd when usdToTryRate.HasValue && usdToTryRate.Value > 0:
                convertedAmount = tryAmount / usdToTryRate.Value;
                return true;
            default:
                convertedAmount = 0;
                return false;
        }
    }
}
