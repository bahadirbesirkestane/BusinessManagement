using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Business.Web.ModelBinding;

public class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext is null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var rawValue = valueResult.FirstValue;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        var normalizedValue = NormalizeDecimalInput(rawValue);
        if (TryParseDecimal(normalizedValue, out var parsedValue))
        {
            bindingContext.Result = ModelBindingResult.Success(parsedValue);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Sayısal değer formatı geçersiz.");
        return Task.CompletedTask;
    }

    private static string NormalizeDecimalInput(string value)
    {
        var normalized = value.Trim().Replace(" ", string.Empty);
        var lastComma = normalized.LastIndexOf(',');
        var lastDot = normalized.LastIndexOf('.');
        var separatorIndex = Math.Max(lastComma, lastDot);

        if (separatorIndex < 0)
        {
            return normalized;
        }

        var integerPart = normalized[..separatorIndex]
            .Replace(".", string.Empty)
            .Replace(",", string.Empty);
        var fractionalPart = normalized[(separatorIndex + 1)..]
            .Replace(".", string.Empty)
            .Replace(",", string.Empty);

        return string.IsNullOrEmpty(fractionalPart)
            ? integerPart
            : $"{integerPart}.{fractionalPart}";
    }

    private static bool TryParseDecimal(string value, out decimal parsedValue)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedValue) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedValue);
    }
}
