using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Business.Web.ModelBinding;

public class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    private static readonly IModelBinder Binder = new FlexibleDecimalModelBinder();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var modelType = context.Metadata.UnderlyingOrModelType;
        return modelType == typeof(decimal) ? Binder : null;
    }
}
