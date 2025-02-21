namespace FspQuery;

public enum FilterConditionType
{
    // https://docs.microsoft.com/en-us/azure/cosmos-db/sql-query-linq-to-sql#SupportedLinqOperators
    Undefined,
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    NotStartsWith,
    EndsWith,
    NotEndsWith,
    GreaterThan,
    NotGreaterThan,
    GreaterThanOrEqual,
    NotGreaterThanOrEqual,
    LessThan,
    NotLessThan,
    LessThanOrEqual,
    NotLessThanOrEqual
}