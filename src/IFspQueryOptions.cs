using System.ComponentModel;

namespace FspQuery;

public interface IFspQueryOptions
{
    int? PageNumber { get; set; }
    int? PageSize { get; set; }
    string? SortPropertyName { get; set; }
    ListSortDirection SortDirection { get; set; }
    HashSet<FilterCondition>? Filters { get; set; }
}