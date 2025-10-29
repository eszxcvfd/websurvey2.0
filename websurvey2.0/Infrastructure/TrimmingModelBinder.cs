using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace websurvey2._0.Infrastructure;

public class TrimmingModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null) throw new ArgumentNullException(nameof(bindingContext));

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (value is null)
            return Task.CompletedTask;

        var trimmed = value.Trim();
        if (bindingContext.ModelType == typeof(string))
        {
            bindingContext.Result = ModelBindingResult.Success(trimmed);
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Success(value);
        }
        return Task.CompletedTask;
    }
}

public class TrimmingModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(string))
        {
            return new TrimmingModelBinder();
        }
        return null;
    }
}