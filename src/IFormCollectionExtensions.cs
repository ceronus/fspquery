using Microsoft.AspNetCore.Http;

namespace FspQuery;

public static class IFormCollectionExtensions
{
    public static bool TryParse<T>(this IFormCollection formCollection, T? result, out string? errorMessage)
        where T : IFspQueryOptions, new()
    {
        result ??= new();
        return TryParse(formCollection, (IFspQueryOptions)result, out errorMessage);
    }

    public static bool TryParse(this IFormCollection formCollection, IFspQueryOptions options, out string? errorMessage)
    {
        return IEnumerableKeyValuePairExtensions.TryParse(formCollection, options, out errorMessage);
    }
}
