namespace FspQuery;

public class FspQueryException : Exception
{
    public string? PropertyAccessor { get; private set; }
    public object? Value { get; private set; }
    public FilterConditionType? FilterConditionType { get; private set; }

    internal FspQueryException(string? message) : base(message) { }

    public FspQueryException(string? propertyAccessor, object? value, FilterConditionType? filterConditionType) : base()
    {
        PropertyAccessor = propertyAccessor;
        Value = value;
        FilterConditionType = filterConditionType;
    }

    public FspQueryException(string? propertyAccessor, object? value, FilterConditionType? filterConditionType, string? message) : base(message)
    {
        PropertyAccessor = propertyAccessor;
        Value = value;
        FilterConditionType = filterConditionType;
    }

    public FspQueryException(string? propertyAccessor, object? value, FilterConditionType? filterConditionType, string? message, Exception innerException) : base(message, innerException)
    {
        PropertyAccessor = propertyAccessor;
        Value = value;
        FilterConditionType = filterConditionType;
    }
}