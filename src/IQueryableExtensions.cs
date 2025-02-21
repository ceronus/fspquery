namespace FspQuery;

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplyPagingFilteringSorting<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplyFilteringSortingPaging(query, operation);

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplyPaging(query, operation);

    public static IQueryable<T> ApplyFiltering<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplyFiltering(query, operation);

    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplySorting(query, operation);
}