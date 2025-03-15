using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace FspQuery;

public class FspQueryLogic : IFspQueryLogic
{
    private readonly IObjectIndexer _indexer;

    public FspQueryLogic(IObjectIndexer indexer)
    {
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
    }

    public IQueryable<T> ApplyFilteringSortingPaging<T>(IQueryable<T> query, IFspQueryOptions options) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);
        query = ApplyFiltering(query, options);
        query = ApplySorting(query, options);
        query = ApplyPaging(query, options); // must come last
        return query;
    }


    /// <remarks>This should be performed AFTER any sorting and filtering</remarks>
    public IQueryable<T> ApplyPaging<T>(IQueryable<T> query, IFspQueryOptions options) where T : class
    {
        // consider: a warning or debug error if paging is applied before any sorting and filtering
        // consider: a warning or debug error if sorting or filtering is applied after paging
        // consider: there might be a legitimate reason to do paging first and then filter or sort (such as sorting a subset of data)
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        int page = options.PageNumber ?? 0;
        int pagesize = options.PageSize ?? 0;

        query = query
            .Skip(pagesize * (page - 1))
            .Take(pagesize);

        return query;
    }

    public IQueryable<T> ApplySorting<T>(IQueryable<T> query, IFspQueryOptions options) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        string? sortPropertyName = options?.SortPropertyName;
        if (string.IsNullOrWhiteSpace(sortPropertyName)) return query;
        return ApplyOrderByListSortDirection(query, sortPropertyName, options?.SortDirection ?? ListSortDirection.Ascending);
    }

    public IOrderedQueryable<T> ApplyOrderByListSortDirection<T>(
        IQueryable<T> query,
        string propertyAccessor,
        ListSortDirection sortDirection) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyAccessor);

        const string discard = "_";
        ParameterExpression parameterExpression = Expression.Parameter(typeof(T), discard);
        Expression propertyExpression = BuildPropertyExpression<T>(propertyAccessor, parameterExpression);
        LambdaExpression lambda = Expression.Lambda(propertyExpression, parameterExpression);

        string methodName = sortDirection == ListSortDirection.Ascending ? nameof(Queryable.OrderBy)
            : sortDirection == ListSortDirection.Descending ? nameof(Queryable.OrderByDescending)
            : throw new NotSupportedException();

        Type propertyType = GetType(typeof(T), propertyAccessor);
        Debug.Assert(propertyType != null);

        MethodCallExpression orderByCallExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), propertyType],
            query.Expression,
            Expression.Quote(lambda));

        return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(orderByCallExpression);
    }

    public IQueryable<T> ApplyFiltering<T>(IQueryable<T> query, IFspQueryOptions options) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Filters == null) return query;

        foreach (FilterCondition filter in options.Filters)
        {
            string propertyAccessor = filter.PropertyAccessor;
            object? filterValue = filter.Value;
            FilterConditionType filterType = filter.FilterConditionType;
            bool ignoreCase = filter.IgnoreCase ?? true;
            query = ApplyFilter(query, propertyAccessor, filterValue, filterType, ignoreCase);
        }

        // consider: to get all the errors and push them out as a single exception

        return query;
    }

    public IQueryable<T> ApplyFilter<T>(IQueryable<T> query, string propertyAccessor, object? value, FilterConditionType filter, bool ignoreCase = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrEmpty(propertyAccessor)) return query;

        Type propertyType = GetType(typeof(T), propertyAccessor);
        Type valueType = value?.GetType() ?? propertyType;

        if (valueType != propertyType)
        {
            try
            {
                Type? underlyingType = Nullable.GetUnderlyingType(propertyType);
                Type type = underlyingType ?? propertyType;
                value = Convert.ChangeType(value?.ToString(), type, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException ex)
            {
                throw new FspQueryException(propertyAccessor, value, filter, $"The type for the filter value {(IsNumericOrNull(valueType) ? $"{value}" : $"\"{value}\"")} is invalid (types do not match) for \"{propertyAccessor}\".", ex);
            }
        }

        try
        {
            Expression<Func<T, bool>> predicate = CreatePredicate<T>(propertyAccessor, propertyType, value, filter, ignoreCase);
            return query.Where(predicate);
        }
        catch (FspQueryException ex)
        {
            throw new FspQueryException(propertyAccessor, value, filter, $"{ex.Message} for property accessor \"{propertyAccessor}\".", ex);
        }
    }

    private static bool IsNumericOrNull(Type? type)
    {
        if (type == null) return true;
        if (type == typeof(int)) return true;
        if (type == typeof(long)) return true;
        if (type == typeof(double)) return true;
        if (type == typeof(decimal)) return true;
        return false;
    }

    private Expression<Func<T, bool>> CreatePredicate<T>(string propertyAccessor, Type propertyType, object? value, FilterConditionType filter, bool ignoreCase) where T : class
    {
        Func<Expression, Expression> expression = propertyType == typeof(string)
            ? CreateStringMethodFilterExpression(value, filter, ignoreCase)
            : CreateNumericOperandsFilterExpression(value, propertyType, filter);

        return CreateExpression<T>(propertyAccessor, expression);
    }

    private Type GetType(Type type, string propertyAccessor)
        => GetMembers(type, propertyAccessor).Last().propertyType;

    private IEnumerable<(Type propertyType, string propertyName)> GetMembers(Type type, string propertyAccessor)
    {
        Type? currentType = type;
        string[] jsonPropertyNames = GetPropertySegments(propertyAccessor);
        for (int i = 0; i < jsonPropertyNames.Length; i++)
        {
            PropertyInfo? propertyInfo = _indexer.GetPropertyInfo(currentType, jsonPropertyNames[i]) ?? throw new FspQueryException($"The property accessor \"{propertyAccessor}\" is invalid.");
            currentType = propertyInfo.PropertyType;

            yield return (currentType, propertyInfo.Name);
        }
    }

    private static string[] GetPropertySegments(string propertyAccessor)
    {
        const char PropertyNameDelimeter = '.'; // dot notation
        return propertyAccessor.Split(PropertyNameDelimeter);
    }

    private static Func<Expression, Expression> CreateStringMethodFilterExpression(object? value, FilterConditionType filter, bool ignoreCase)
    {
        // Only use the Cosmos DB supported LINQ methods
        // https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/linq-to-sql#supported-linq-operators
        if (value == null && (filter == FilterConditionType.Contains
            || filter == FilterConditionType.NotContains
            || filter == FilterConditionType.StartsWith
            || filter == FilterConditionType.NotStartsWith
            || filter == FilterConditionType.EndsWith
            || filter == FilterConditionType.NotEndsWith
            ))
        {
            throw new FspQueryException($"The filter value NULL cannot be used with the filter condition \"{filter}\"");
        }

        bool isNot = false;
#pragma warning disable IDE0010 // Add missing cases
        switch (filter)
        {
            case FilterConditionType.Equals:
            case FilterConditionType.Contains:
            case FilterConditionType.StartsWith:
            case FilterConditionType.EndsWith:
                break;
            case FilterConditionType.NotEquals:
                filter = FilterConditionType.Equals;
                isNot = true;
                break;
            case FilterConditionType.NotContains:
                filter = FilterConditionType.Contains;
                isNot = true;
                break;
            case FilterConditionType.NotStartsWith:
                filter = FilterConditionType.StartsWith;
                isNot = true;
                break;
            case FilterConditionType.NotEndsWith:
                filter = FilterConditionType.EndsWith;
                isNot = true;
                break;
            default:
                throw new FspQueryException($"The filter condition \"{filter}\" cannot be used with the data type");
        }
#pragma warning restore IDE0010 // Add missing cases

        ConstantExpression constantExpression = Expression.Constant(value, typeof(string));

        if (ignoreCase)
        {
            MethodInfo methodInfo = typeof(string).GetMethod(filter.ToString(), [typeof(string), typeof(StringComparison)])!;

            return isNot
            ? (propertyExpression) => Expression.Not(Expression.Call(propertyExpression, methodInfo, constantExpression, Expression.Constant(StringComparison.InvariantCultureIgnoreCase)))
            : (propertyExpression) => Expression.Call(propertyExpression, methodInfo, constantExpression, Expression.Constant(StringComparison.InvariantCultureIgnoreCase));
        }
        else
        {
            MethodInfo methodInfo = typeof(string).GetMethod(filter.ToString(), [typeof(string)])!;
            return isNot
            ? (propertyExpression) => Expression.Not(Expression.Call(propertyExpression, methodInfo, constantExpression))
            : (propertyExpression) => Expression.Call(propertyExpression, methodInfo, constantExpression);
        }
    }

    private static Func<Expression, Expression> CreateNumericOperandsFilterExpression(object? value, Type type, FilterConditionType filter)
    {
        // Only use the Cosmos DB supported LINQ methods
        // https://docs.microsoft.com/en-us/azure/cosmos-db/sql-query-linq-to-sql#SupportedLinqOperators
        if (value == null && (filter == FilterConditionType.GreaterThan
            || filter == FilterConditionType.NotGreaterThan
            || filter == FilterConditionType.GreaterThanOrEqual
            || filter == FilterConditionType.NotGreaterThanOrEqual
            || filter == FilterConditionType.LessThan
            || filter == FilterConditionType.NotLessThan
            || filter == FilterConditionType.LessThanOrEqual
            || filter == FilterConditionType.NotLessThanOrEqual
            ))
        {
            throw new FspQueryException($"The filter value NULL cannot be used with the filter condition \"{filter}\"");
        }

        ConstantExpression constantExpression = Expression.Constant(value, type);
#pragma warning disable IDE0072 // Add missing cases
        return filter switch
        {
            FilterConditionType.Equals => (propertyExpression) => Expression.Equal(propertyExpression, constantExpression),
            FilterConditionType.NotEquals => (propertyExpression) => Expression.Not(Expression.Equal(propertyExpression, constantExpression)),
            FilterConditionType.GreaterThan => (propertyExpression) => Expression.GreaterThan(propertyExpression, constantExpression),
            FilterConditionType.NotGreaterThan => (propertyExpression) => Expression.Not(Expression.GreaterThan(propertyExpression, constantExpression)),
            FilterConditionType.GreaterThanOrEqual => (propertyExpression) => Expression.GreaterThanOrEqual(propertyExpression, constantExpression),
            FilterConditionType.NotGreaterThanOrEqual => (propertyExpression) => Expression.Not(Expression.GreaterThanOrEqual(propertyExpression, constantExpression)),
            FilterConditionType.LessThan => (propertyExpression) => Expression.LessThan(propertyExpression, constantExpression),
            FilterConditionType.NotLessThan => (propertyExpression) => Expression.Not(Expression.LessThan(propertyExpression, constantExpression)),
            FilterConditionType.LessThanOrEqual => (propertyExpression) => Expression.LessThanOrEqual(propertyExpression, constantExpression),
            FilterConditionType.NotLessThanOrEqual => (propertyExpression) => Expression.Not(Expression.LessThanOrEqual(propertyExpression, constantExpression)),
            _ => throw new FspQueryException($"The filter condition \"{filter}\" cannot be used with the data type"),
        };
#pragma warning restore IDE0072 // Add missing cases
    }

    private Expression<Func<T, bool>> CreateExpression<T>(string propertyAccessor, Func<Expression, Expression> expression) where T : class
    {
        const string discard = "_";
        ParameterExpression parameterExpression = Expression.Parameter(typeof(T), discard);
        Expression propertyExpression = BuildPropertyExpression<T>(propertyAccessor, parameterExpression);
        Expression filterExpression = expression(propertyExpression);

        return Expression.Lambda<Func<T, bool>>(filterExpression, parameterExpression);
    }

    private Expression BuildPropertyExpression<T>(string propertyAccessor, ParameterExpression parameterExpression) where T : class
    {
        Expression propertyExpression = parameterExpression;
        foreach (string propertyName in GetMembers(typeof(T), propertyAccessor).Select(_ => _.propertyName))
        {
            propertyExpression = Expression.PropertyOrField(propertyExpression, propertyName);
        }
        return propertyExpression;
    }
}