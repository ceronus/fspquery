using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FspQuery;

public class ObjectIndexer : IObjectIndexer
{
    private readonly JsonSerializerOptions _options;
    private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _proertyInfos;

    public ObjectIndexer() : this(CreateDefaultJsonSerializerOptions()) { }

    public ObjectIndexer(JsonSerializerOptions options)
    {
        _proertyInfos = [];
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void PreloadObject<T>()
        where T : class
    {
        Add(typeof(T));
    }

    public bool ContainsPropertyName(Type type, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return false;

        return this[type].ContainsKey(propertyName);
    }

    public bool ContainsPropertyName<T>(string? propertyName)
        => ContainsPropertyName(typeof(T), propertyName);

    public PropertyInfo? GetPropertyInfo(Type type, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return null;
        if (!ContainsPropertyName(type, propertyName)) return null;

        return this[type][propertyName];
    }

    public PropertyInfo? GetPropertyInfo<T>(string? propertyName)
        => GetPropertyInfo(typeof(T), propertyName);

    public string? GetPropertyName(Type type, string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return null;
        if (!ContainsPropertyName(type, propertyName)) return null;

        return this[type][propertyName].Name;
    }

    public string? GetPropertyName<T>(string? propertyName)
        => GetPropertyName(typeof(T), propertyName);

    public Type? GetPropertyType(Type type, string? propertyName)
        => GetPropertyInfo(type, propertyName)?.PropertyType;

    public Type? GetPropertyType<T>(string? propertyName)
        => GetPropertyType(typeof(T), propertyName);

    public bool TrySetValue<T>(JsonElement element, T original, string? propertyName)
    {
        if (original == null || string.IsNullOrEmpty(propertyName))
            return false;

        try
        {
            SetValue(element, original, propertyName);
            return true;
        }
        catch (JsonException)
        {
            // complex type failure
            return false;
        }
        catch (InvalidOperationException)
        {
            // type mismatch, request object sent the wrong type
            return false;
        }
        catch
        {
            return false;
        }
    }

    private readonly object _setLock = new();

    public void SetValue<T>(JsonElement element, T original, string? propertyName)
    {
        lock (_setLock)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

            object? value;

            Type valueType = typeof(T);

            // if the type was object, determine the type
            if (valueType == typeof(object))
            {
                valueType = original.GetType();

                // unable to determine the type
                if (valueType == typeof(object)) throw new ArgumentException($"The value type {{{valueType}}} is not a supported type.");
            }

            // add the type to the registry
            Add(valueType);

            // check if the property is read-only (not setter)
            if (_proertyInfos[valueType][propertyName].GetSetMethod() == null) throw new ArgumentException($"{{{propertyName}}} does not have a set method (it is read-only).");

            // store the property type
            Type propertyType = _proertyInfos[valueType][propertyName].PropertyType;

            // check the json value
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    value = null;
                    goto SetValue;
                case JsonValueKind.True:
                    value = true;
                    goto SetValue;
                case JsonValueKind.False:
                    value = false;
                    goto SetValue;
                case JsonValueKind.String:
                case JsonValueKind.Number:
                    goto ParsePrimitives;
                case JsonValueKind.Object:
                    goto ParseObject;
                case JsonValueKind.Array:
                    goto ParseGeneric;
                case JsonValueKind.Undefined:
                default:
                    throw new ArgumentException($"The value kind '{element.ValueKind}' is not supported.");
            }

        ParsePrimitives:
            // primitive types
            switch (propertyType.ToString())
            {
                case "System.String":
                    value = element.GetString();
                    goto SetValue;
                case "System.DateTime":
                    value = element.GetDateTime();
                    goto SetValue;
                case "System.DateTimeOffset":
                    value = element.GetDateTimeOffset();
                    goto SetValue;
                case "System.Boolean":
                    value = element.GetBoolean();
                    goto SetValue;
                case "System.Guid":
                    value = element.GetGuid();
                    goto SetValue;
                case "System.Double":
                    value = element.GetDouble();
                    goto SetValue;
                case "System.Decimal":
                    value = element.GetDecimal();
                    goto SetValue;
                case "System.Int16":
                    value = element.GetInt16();
                    goto SetValue;
                case "System.Int32":
                    value = element.GetInt32();
                    goto SetValue;
                case "System.Int64":
                    value = element.GetInt64();
                    goto SetValue;
                default:
                    // check if the property is nullable
                    Type? underlyingType = Nullable.GetUnderlyingType(propertyType);
                    if (underlyingType != null)
                    {
                        // strip the nullable attribute
                        // there is no need to worry about the value being null as this is already 
                        // checked previously see: "switch (element.ValueKind)"
                        propertyType = underlyingType;
                        goto ParsePrimitives;
                    }

                    // check if the property is an enum
                    if (propertyType?.BaseType?.FullName == "System.Enum")
                    {
                        string? enumStringValue = element.GetString();
                        if (!Enum.TryParse(propertyType, enumStringValue, out value))
                            throw new ArgumentException($"The value {{{enumStringValue}}} is invalid.");
                        goto SetValue;
                    }

                    // unknown type, try to handle it using our generic parser
                    goto ParseGeneric;
            }

        ParseObject:
            // complex types
            {
                Type type = _proertyInfos[valueType][propertyName].PropertyType;

                // add the type to the register
                Add(type);

                // if the property is has an inner object with no properties
                if (_proertyInfos[type].Count == 0) goto ParseGeneric;

                {
                    using JsonDocument doc = JsonDocument.Parse(element.GetRawText());

                    // perform the custom PATCH operation
                    foreach (JsonProperty jsonProperty in doc.RootElement.EnumerateObject())
                    {
                        string innerElementPropertyName = _proertyInfos[valueType][propertyName].Name;
                        object? innerElementObject = original.GetType()?.GetProperty(innerElementPropertyName)?.GetValue(original, null);

                        // in the event that the inner element of the current database object is null, we need to new one up 
                        if (innerElementObject == null)
                        {
                            // new up the object
                            Type? innerElementType = original.GetType()?.GetProperty(innerElementPropertyName)?.PropertyType;
                            Debug.Assert(innerElementType is not null);
                            original.GetType()?.GetProperty(innerElementPropertyName)?.SetValue(original, Activator.CreateInstance(innerElementType));
                            innerElementObject = original.GetType()?.GetProperty(innerElementPropertyName)?.GetValue(original, null);
                        }

                        JsonElement innerElement = doc.RootElement.GetProperty(jsonProperty.Name);
                        SetValue(innerElement, innerElementObject, jsonProperty.Name);
                    }

                    return;
                }
            }

        ParseGeneric:
            {
                string json = element.GetRawText();

                // special case to handle nullable structs
                Debug.Assert(propertyType is not null);
                Type? underlyingType = Nullable.GetUnderlyingType(propertyType);
                value = underlyingType != null
                    ? JsonSerializer.Deserialize(json, underlyingType, _options)
                    : JsonSerializer.Deserialize(json, propertyType, _options);
            }

        // set the value
        SetValue:
            _proertyInfos[valueType][propertyName].SetValue(original, value);
            return;
        }
    }


    private bool _isAddLocked = false;
    private readonly object _addLock = new();
    private ManualResetEventSlim _addLockEvent = new(false);

    public Dictionary<string, PropertyInfo> this[Type propertyType]
    {
        get
        {
            if (!_proertyInfos.ContainsKey(propertyType))
            {
                Add(propertyType);
            }
            else if (_isAddLocked)
            {
                _addLockEvent.Wait();
            }
            return _proertyInfos[propertyType];
        }
    }

    private void Add(Type propertyType)
    {
        ArgumentNullException.ThrowIfNull(propertyType);

        lock (_addLock)
        {
            try
            {
                _isAddLocked = true;
                _addLockEvent.Reset();

                // register already has the type mapped
                if (_proertyInfos.ContainsKey(propertyType)) return;

                // add the property to the register
                _proertyInfos.Add(propertyType, []);

                // populate dictionary
                foreach (PropertyInfo propertyInfo in propertyType.GetProperties())
                {
                    string propertyName = propertyInfo.Name;

                    // iterate over the custom attributes
                    foreach (CustomAttributeData attribute in propertyInfo.CustomAttributes)
                    {
                        // check if the attribute is JsonPropertyName
                        if (attribute.AttributeType == typeof(JsonPropertyNameAttribute))
                        {
                            // the constructor should be JsonPropertyName(string value)
                            // meaning there is only one (1) argument
                            if (attribute.ConstructorArguments.Count != 1)
                                throw new InvalidOperationException("The constructor for the attribute does not match JsonPropertyName(string value)");

                            propertyName = attribute.ConstructorArguments[0].Value?.ToString() ?? throw new InvalidOperationException();

                            // no longer need to iterate once the attribute is found
                            // skip to the next property
                            break;
                        }
                    }

                    // add the property name
                    _proertyInfos[propertyType].Add(propertyName, propertyInfo);
                }
            }
            finally
            {
                _isAddLocked = false;
                _addLockEvent.Set();
            }
        }
    }

    private static JsonSerializerOptions CreateDefaultJsonSerializerOptions()
    {
        return new()
        {
            MaxDepth = 2048,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}