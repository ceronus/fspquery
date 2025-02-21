using System.Text.Json.Serialization;

namespace FspQuery;
public struct FilterCondition
{
    private const bool DefaultIgnoreCase = true;

    [JsonPropertyName("propertyAccessor")]
    public string PropertyAccessor { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("condition")]
    public FilterConditionType FilterConditionType { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("ignoreCase")]
    public bool? IgnoreCase { get; set; } = DefaultIgnoreCase;

    public FilterCondition(string propertyAccessor, FilterConditionType filterConditionType, object? value, bool ignoreCase = DefaultIgnoreCase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyAccessor);
        PropertyAccessor = propertyAccessor;
        FilterConditionType = filterConditionType;
        Value = value;
        IgnoreCase = ignoreCase;
    }

    public static FilterCondition CreateEquals(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.Equals, value);
    public static FilterCondition CreateContains(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.Contains, value);
    public static FilterCondition CreateNotContains(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.NotContains, value);
    public static FilterCondition CreateStartsWith(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.StartsWith, value);
    public static FilterCondition CreateNotStartsWith(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.NotStartsWith, value);
    public static FilterCondition CreateEndsWith(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.EndsWith, value);
    public static FilterCondition CreateNotEndsWith(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.NotEndsWith, value);
    public static FilterCondition CreateGreaterThan(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.GreaterThan, value);
    public static FilterCondition CreateGreaterThanOrEqual(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.GreaterThanOrEqual, value);
    public static FilterCondition CreateNotGreaterThanOrEqual(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.NotGreaterThanOrEqual, value);
    public static FilterCondition CreateLessThan(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.LessThan, value);
    public static FilterCondition CreateNotLessThan(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.NotLessThan, value);
    public static FilterCondition CreateLessThanOrEqual(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.LessThanOrEqual, value);
    public static FilterCondition CreateNotLessThanOrEqual(string propertyAccessor, object? value) => new(propertyAccessor, FilterConditionType.NotLessThanOrEqual, value);
}