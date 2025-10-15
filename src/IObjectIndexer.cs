using System.Reflection;
using System.Text.Json;

namespace FspQuery;

public interface IObjectIndexer
{
    Dictionary<string, PropertyInfo> this[Type propertyType] { get; }
    public void PreloadObject<T>() where T : class;
    bool ContainsPropertyName(Type type, string? jsonPropertyName);
    bool ContainsPropertyName<T>(string? jsonPropertyName);
    PropertyInfo? GetPropertyInfo(Type type, string? jsonPropertyName);
    PropertyInfo? GetPropertyInfo<T>(string? jsonPropertyName);
    string? GetPropertyName(Type type, string? jsonPropertyName);
    string? GetPropertyName<T>(string? jsonPropertyName);
    Type? GetPropertyType(Type type, string? jsonPropertyName);
    Type? GetPropertyType<T>(string? jsonPropertyName);
    bool TrySetValue<T>(JsonElement element, T original, string? jsonPropertyName);
    void SetValue<T>(JsonElement element, T original, string? jsonPropertyName);
}