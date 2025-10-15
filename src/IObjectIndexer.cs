using System.Reflection;
using System.Text.Json;

namespace FspQuery;

public interface IObjectIndexer
{
    Dictionary<string, PropertyInfo> this[Type propertyType] { get; }
    public void PreloadObject<T>() where T : class;
    bool ContainsPropertyName(Type type, string? propertyName);
    bool ContainsPropertyName<T>(string? propertyName);
    PropertyInfo? GetPropertyInfo(Type type, string? propertyName);
    PropertyInfo? GetPropertyInfo<T>(string? propertyName);
    string? GetPropertyName(Type type, string? propertyName);
    string? GetPropertyName<T>(string? propertyName);
    Type? GetPropertyType(Type type, string? propertyName);
    Type? GetPropertyType<T>(string? propertyName);
    bool TrySetValue<T>(JsonElement element, T original, string? propertyName);
    void SetValue<T>(JsonElement element, T original, string? propertyName);
}