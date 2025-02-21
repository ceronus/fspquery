using System.ComponentModel;

namespace FspQuery;

public class FspQueryValidator : IFspQueryValidator
{
    private readonly IObjectIndexer _indexer;

    public FspQueryValidator(IObjectIndexer indexer)
    {
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
    }

    public bool ValidateWithDefaults<T>(IFspQueryOptions options, out string? errorMessage)
        => ValidateWithDefaults<T>(options, null, out errorMessage);

    public bool ValidateWithDefaults<T>(IFspQueryOptions options, int? maximumPageSize, out string? errorMessage)
    {
        if (!ValidatePageNumber(options, out errorMessage)) return false;
        if (!ValidatePageSize(options, out errorMessage, maximum: maximumPageSize)) return false;
        if (!ValidateFilters<T>(options, out errorMessage)) return false;
        return true;
    }

    public bool ValidatePageNumber(IFspQueryOptions options, out string? errorMessage, int? minimum = 0, int? maximum = null)
    {
        // default out value
        errorMessage = null;

        int page = options.PageNumber ?? 0;

        if (minimum != null && page <= minimum)
        {
            errorMessage = $"Invalid or malformed value for \"{nameof(page)}\". Must be a value greater than {minimum}.";
            return false;
        }

        if (maximum != null && page > maximum)
        {
            errorMessage = $"Invalid or malformed value for \"{nameof(page)}\". Must be a value less than {maximum}.";
            return false;
        }

        return true;
    }

    public bool ValidatePageSize(IFspQueryOptions options, out string? errorMessage, int? minimum = 0, int? maximum = null)
    {
        // default out value
        errorMessage = null;

        int pagesize = options.PageSize ?? 0;

        if (minimum != null && pagesize <= minimum)
        {
            errorMessage = $"Invalid or malformed value for \"{nameof(pagesize)}\". Must be a value greater than {minimum}.";
            return false;
        }

        if (maximum != null && pagesize > maximum)
        {
            errorMessage = $"Invalid or malformed value for \"{nameof(pagesize)}\". Must be a value less than {maximum}.";
            return false;
        }

        return true;
    }

    public bool ValidateFilters<T>(IFspQueryOptions options, out string? errorMessage)
        => ValidateFilters<T>(options, _indexer, out errorMessage);


    internal static bool ValidateFilters<T>(IFspQueryOptions operation, IObjectIndexer indexer, out string? errorMessage)
    {
        // default out value
        errorMessage = null;

        if (operation.Filters == null || operation.Filters.Count == 0) return true;

        foreach (FilterCondition filter in operation.Filters)
        {
            string propertyAccessor = filter.PropertyAccessor;
            if (string.IsNullOrEmpty(propertyAccessor))
            {
                errorMessage = $"Invalid or malformed filter property accessor.";
                return false;
            }

            FilterConditionType condition = filter.FilterConditionType;
            if (condition == FilterConditionType.Undefined)
            {
                errorMessage = $"Invalid or malformed filter condition value.";
                return false;
            }

            object? propertyValue = filter.Value;
            if (propertyValue is null)
            {
                errorMessage = $"Invalid or malformed filter value for \"{propertyAccessor}\". Value is empty.";
                return false;
            }

            // skip invalid property names
            if (!indexer.ContainsPropertyName<T>(propertyAccessor)) continue;

            try
            {
                // validate the type is matching (i.e. string, int, etc)
                Type? propertyType = indexer.GetPropertyType<T>(propertyAccessor);
                if (propertyType == null) return false;
                TypeConverter typeConverter = TypeDescriptor.GetConverter(propertyType);
                _ = typeConverter.ConvertFrom(propertyValue);
            }
            catch (ArgumentException)
            {
                errorMessage = $"Invalid or malformed filter value for \"{propertyAccessor}\". Type does not match.";
                return false;
            }
            catch (FormatException)
            {
                errorMessage = $"Invalid or malformed filter value for \"{propertyAccessor}\". Format does not match.";
                return false;
            }
        }

        return true;
    }
}