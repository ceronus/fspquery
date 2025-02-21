
namespace FspQuery;

public interface IFspQueryValidator
{
    bool ValidateFilters<T>(IFspQueryOptions options, out string? errorMessage);
    bool ValidatePageNumber(IFspQueryOptions options, out string? errorMessage, int? minimum = 0, int? maximum = null);
    bool ValidatePageSize(IFspQueryOptions options, out string? errorMessage, int? minimum = 0, int? maximum = null);
    bool ValidateWithDefaults<T>(IFspQueryOptions options, out string? errorMessage);
    bool ValidateWithDefaults<T>(IFspQueryOptions options, int? maximumPageSize, out string? errorMessage);
}