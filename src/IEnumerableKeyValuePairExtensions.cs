using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace FspQuery;

public static class IEnumerableKeyValuePairExtensions
{
    private const int DefaultPageSize = 100;
    private const ListSortDirection DefaultSortDirection = ListSortDirection.Ascending;

    private const string SortAscendingValue = "asc";
    private const string SortDescendingValue = "desc";
    private const string PageNumberKey = "page";
    private const string PageSizeKey = "pagesize";
    private const string SortDirectionKey = "order";
    private const string SortPropertyNameKey = "sort";

    // string filter condition types
    private const string ContainsPrefix = $"in^";
    private const string NotContainsPrefix = $"!in^";
    private const string StartsWithPrefix = $"pre^";
    private const string NotStartsWithPrefix = $"!pre^";
    private const string EndsWithPrefix = $"end^";
    private const string NotEndsWithPrefix = $"!end^";

    // numeric filter condition types
    private const string GreaterThanPrefix = $"gt^";
    private const string NotGreaterThanPrefix = $"!gt^";
    private const string GreaterThanOrEqualPrefix = $"gte^";
    private const string NotGreaterThanOrEqualPrefix = $"!gte^";
    private const string LessThanPrefix = $"lt^";
    private const string NotLessThanPrefix = $"!lt^";
    private const string LessThanOrEqualPrefix = $"lte^";
    private const string NotLessThanOrEqualPrefix = $"!lte^";

    // combo
    private const string EqualsPrefix = $"eq^";
    private const string NotEqualsPrefix = $"!eq^";

    private static readonly Dictionary<string, FilterConditionType> _map = new()
    {
        { EqualsPrefix, FilterConditionType.Equals },
        { NotEqualsPrefix, FilterConditionType.NotEquals },
        { ContainsPrefix, FilterConditionType.Contains },
        { NotContainsPrefix, FilterConditionType.NotContains },
        { StartsWithPrefix, FilterConditionType.StartsWith },
        { NotStartsWithPrefix, FilterConditionType.NotStartsWith },
        { EndsWithPrefix, FilterConditionType.EndsWith },
        { NotEndsWithPrefix, FilterConditionType.NotEndsWith },
        { GreaterThanPrefix, FilterConditionType.GreaterThan },
        { NotGreaterThanPrefix, FilterConditionType.NotGreaterThan },
        { GreaterThanOrEqualPrefix, FilterConditionType.GreaterThanOrEqual },
        { NotGreaterThanOrEqualPrefix, FilterConditionType.NotGreaterThanOrEqual },
        { LessThanPrefix, FilterConditionType.LessThan },
        { NotLessThanPrefix, FilterConditionType.NotLessThan },
        { LessThanOrEqualPrefix, FilterConditionType.LessThanOrEqual },
        { NotLessThanOrEqualPrefix, FilterConditionType.NotLessThanOrEqual },
    };


    public static bool TryParse<T>(this IEnumerable<KeyValuePair<string, StringValues>> queries, T? result, out string? errorMessage)
        where T : IFspQueryOptions, new()
    {
        result ??= new();
        return TryParse(queries, (IFspQueryOptions)result, out errorMessage);
    }

    public static bool TryParse(this IEnumerable<KeyValuePair<string, StringValues>> queries, IFspQueryOptions options, out string? errorMessage)
    {
        options.AssignMissingDefaultValues();

        foreach (KeyValuePair<string, StringValues> query in queries)
        {
            string? key = query.Key?.ToLowerInvariant();
            string value = query.Value.ToString();

            // check for missing key
            if (key == null)
            {
                errorMessage = $"Invalid or malformed query.";
                return false;
            }

            // check for duplicate keys
            else if (query.Value.Count > 1)
            {
                errorMessage = $"Invalid or malformed query. Duplicate entry found for property accessor \"{key}\".";
                return false;
            }

            // get the page number
            else if (key.Equals(PageNumberKey, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, out int page))
                {
                    errorMessage = $"Invalid or malformed value for property accessor \"{key}\". Not an integer value.";
                    return false;
                }

                options.PageNumber = page;
            }

            // get the page size
            else if (key.Equals(PageSizeKey, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, out int pagesize))
                {
                    errorMessage = $"Invalid or malformed value for property accessor \"{key}\". Not an integer value.";
                    return false;
                }

                options.PageSize = pagesize;
            }

            // get the sort direction
            else if (key.Equals(SortDirectionKey, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(value, SortAscendingValue, StringComparison.OrdinalIgnoreCase))
                    options.SortDirection = ListSortDirection.Ascending;
                else if (string.Equals(value, SortDescendingValue, StringComparison.OrdinalIgnoreCase))
                {
                    options.SortDirection = ListSortDirection.Descending;
                }
                else if (string.IsNullOrEmpty(value))
                {
                    // use the default value
                    options.SortDirection = DefaultSortDirection;
                }
                else
                {
                    errorMessage = $"Invalid or malformed value for \"{key}\". Acceptable values are \"{SortAscendingValue}\" and \"{SortDescendingValue}\".";
                    return false;
                }
            }

            // get the sort property name
            else if (key.Equals(SortPropertyNameKey, StringComparison.OrdinalIgnoreCase))
            {
                options.SortPropertyName = value;
            }

            // get the filters
            else
            {
                // default uses exact match (i.e. ?name=foo)
                string propertyAccessor = key;
                FilterConditionType conditionType = FilterConditionType.Equals;

                // prefixed key (i.e. ?in^name=foo)
                foreach ((string prefix, FilterConditionType filter) in _map)
                {
                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        propertyAccessor = key[prefix.Length..];
                        conditionType = filter;
                        break;
                    }
                }

                Debug.Assert(options.Filters is not null);
                if (!options.Filters.Add(new(propertyAccessor, conditionType, value)))
                {
                    errorMessage = $"Invalid or malformed value for \"{key}\". Conflicting filter with property accessor \"{propertyAccessor}\".";
                    return false;
                }
            }
        }

        // done
        errorMessage = null;
        return true;
    }
}