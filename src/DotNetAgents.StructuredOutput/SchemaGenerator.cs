using System.Reflection;
using System.Text.Json;

namespace DotNetAgents.StructuredOutput;

/// <summary>
/// Generates JSON Schema from C# types (reflection-based).
/// </summary>
public static class SchemaGenerator
{
    /// <summary>Generate a JSON Schema definition for type T.</summary>
    public static JsonSchemaDefinition FromType<T>()
    {
        return FromType(typeof(T));
    }

    /// <summary>Generate a JSON Schema definition for the given type.</summary>
    public static JsonSchemaDefinition FromType(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        var t = nullable ?? type;
        if (t == typeof(string))
            return new JsonSchemaDefinition { Type = "string" };
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
            return new JsonSchemaDefinition { Type = "integer" };
        if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
            return new JsonSchemaDefinition { Type = "number" };
        if (t == typeof(bool))
            return new JsonSchemaDefinition { Type = "boolean" };
        if (t.IsEnum)
        {
            var names = Enum.GetNames(t);
            var values = Enum.GetValues(t);
            var list = new List<object?>();
            for (var i = 0; i < values.Length; i++)
            {
                var v = values.GetValue(i);
                list.Add(v is int n ? n : v?.ToString());
            }
            return new JsonSchemaDefinition { Type = "string", Enum = list };
        }
        if (t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)) || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)))
        {
            var elem = t.IsArray ? t.GetElementType()! : t.GetGenericArguments()[0];
            return new JsonSchemaDefinition { Type = "array", Items = FromType(elem) };
        }
        if (t.IsClass || t.IsValueType)
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var properties = new Dictionary<string, JsonSchemaDefinition>();
            var required = new List<string>();
            foreach (var p in props)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                var propSchema = FromType(p.PropertyType);
                properties[p.Name] = propSchema;
                var isNullable = Nullable.GetUnderlyingType(p.PropertyType) != null;
                var isRequired = p.PropertyType.IsValueType && !isNullable;
                if (isRequired)
                    required.Add(p.Name);
            }
            return new JsonSchemaDefinition { Type = "object", Properties = properties, Required = required.Count > 0 ? required : null };
        }
        return new JsonSchemaDefinition { Type = "string" };
    }
}
