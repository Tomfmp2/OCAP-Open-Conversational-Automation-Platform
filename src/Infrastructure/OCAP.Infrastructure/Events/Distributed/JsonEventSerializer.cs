using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using OCAP.Core.Events.Distributed;

namespace OCAP.Infrastructure.Events.Distributed;

public class JsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> FullConstructors = new();

    public string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public T Deserialize<T>(string payload)
    {
        return (T)Deserialize(payload, typeof(T))!;
    }

    public object? Deserialize(string payload, Type type)
    {
        // Los eventos son records con un constructor completo y otro corto (que genera EventId y la hora): System.Text.Json
        // no elige entre dos y fallaba en cada mensaje recibido. Se usa el completo, que conserva EventId y la hora.
        var constructor = FullConstructors.GetOrAdd(type, FullConstructor);
        if (constructor is null) return JsonSerializer.Deserialize(payload, type, _options);

        using var document = JsonDocument.Parse(payload);
        var fields = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);
        var arguments = constructor.GetParameters()
            .Select(p => fields.TryGetValue(p.Name!, out var value)
                ? value.Deserialize(p.ParameterType, _options)
                : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)
            .ToArray();
        return constructor.Invoke(arguments);
    }

    /// <summary>El constructor con más parámetros cuando hay varios públicos y ninguno es el por defecto.</summary>
    private static ConstructorInfo? FullConstructor(Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length < 2 || constructors.Any(c => c.GetParameters().Length == 0)) return null;
        return constructors.MaxBy(c => c.GetParameters().Length);
    }
}
