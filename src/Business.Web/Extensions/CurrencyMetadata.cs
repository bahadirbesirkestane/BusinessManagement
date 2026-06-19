using Microsoft.AspNetCore.Mvc.Rendering;

namespace Business.Web.Extensions;

public static class CurrencyMetadata
{
    public const string Try = "TRY";
    public const string Eur = "EUR";
    public const string Usd = "USD";

    private static readonly string[] SupportedCurrencies = [Try, Eur, Usd];

    public static IReadOnlyList<SelectListItem> GetSelectList()
    {
        return
        [
            new SelectListItem("Türk Lirası (TRY)", Try),
            new SelectListItem("Euro (EUR)", Eur),
            new SelectListItem("Amerikan Doları (USD)", Usd)
        ];
    }

    public static string NormalizeInput(string? currency, string defaultCurrency = Try)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return defaultCurrency;
        }

        var normalized = currency.Trim().ToUpperInvariant();
        return SupportedCurrencies.Contains(normalized) ? normalized : defaultCurrency;
    }

    public static string NormalizeStored(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency)
            ? Try
            : currency.Trim().ToUpperInvariant();
    }
}
