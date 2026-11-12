using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Projeto.Api.Binding;

/// <summary>Usa <see cref="DateTimeOffsetComOffsetModelBinder"/> para todo DateTimeOffset(?) vindo da URL.</summary>
internal sealed class DateTimeOffsetComOffsetModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.UnderlyingOrModelType == typeof(DateTimeOffset)
            ? new DateTimeOffsetComOffsetModelBinder()
            : null;
    }
}
