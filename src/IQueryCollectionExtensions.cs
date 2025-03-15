using Microsoft.AspNetCore.Http;

namespace FspQuery;

public static class IQueryCollectionExtensions
{
    public static bool TryParse<T>(this IQueryCollection queryCollection, T? result, out string? errorMessage)
        where T : IFspQueryOptions, new()
    {
        result ??= new();
        return TryParse(queryCollection, (IFspQueryOptions)result, out errorMessage);
    }

    public static bool TryParse(this IQueryCollection queryCollection, IFspQueryOptions options, out string? errorMessage)
    {
        return IEnumerableKeyValuePairExtensions.TryParse(queryCollection, options, out errorMessage);
    }
}