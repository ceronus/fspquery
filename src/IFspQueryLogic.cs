namespace FspQuery;

public interface IFspQueryLogic
{
    IQueryable<T> ApplyFilteringSortingPaging<T>(IQueryable<T> query, IFspQueryOptions options) where T : class;
    IQueryable<T> ApplyPaging<T>(IQueryable<T> query, IFspQueryOptions options) where T : class;
    IQueryable<T> ApplySorting<T>(IQueryable<T> query, IFspQueryOptions options) where T : class;
    IQueryable<T> ApplyFiltering<T>(IQueryable<T> query, IFspQueryOptions options) where T : class;
}