using System.ComponentModel;

namespace FspQuery;

public static class IFspQueryOptionsExtensions
{
    private const int DefaultPageSize = 100;
    private const ListSortDirection DefaultSortDirection = ListSortDirection.Ascending;

    public static IFspQueryOptions AssignMissingDefaultValues(this IFspQueryOptions options)
    {
        if (options.PageNumber == default) options.PageNumber = 1;
        if (options.PageSize == default) options.PageSize = DefaultPageSize;
        if (options.SortPropertyName == default) options.SortPropertyName = null; // redundant
        if (options.SortDirection == default) options.SortDirection = DefaultSortDirection; // redundant
        if (options.Filters == default) options.Filters = [];
        return options;
    }
}