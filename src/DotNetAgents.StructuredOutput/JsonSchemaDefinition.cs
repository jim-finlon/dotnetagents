// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace DotNetAgents.StructuredOutput;

/// <summary>
/// JSON Schema definition for structured output. Can be serialized to provider-specific format (e.g. OpenAI).
/// </summary>
public sealed class JsonSchemaDefinition
{
    /// <summary>Schema type: "object", "array", "string", "number", "integer", "boolean", "null".</summary>
    public string Type { get; init; } = "object";

    /// <summary>Description.</summary>
    public string? Description { get; init; }

    /// <summary>Properties for type "object".</summary>
    public IReadOnlyDictionary<string, JsonSchemaDefinition>? Properties { get; init; }

    /// <summary>Required property names for type "object".</summary>
    public IReadOnlyList<string>? Required { get; init; }

    /// <summary>Item schema for type "array".</summary>
    public JsonSchemaDefinition? Items { get; init; }

    /// <summary>Enum values (for string or number).</summary>
    public IReadOnlyList<object?>? Enum { get; init; }

    /// <summary>Minimum (number).</summary>
    public double? Minimum { get; init; }

    /// <summary>Maximum (number).</summary>
    public double? Maximum { get; init; }

    /// <summary>Min length (string/array).</summary>
    public int? MinLength { get; init; }

    /// <summary>Max length (string/array).</summary>
    public int? MaxLength { get; init; }

    /// <summary>Pattern (string, regex).</summary>
    public string? Pattern { get; init; }

    /// <summary>Convert to a JSON object for OpenAI response_format json_schema.</summary>
    public JsonElement ToJsonElement()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            WriteTo(writer);
        }
        stream.Position = 0;
        return JsonDocument.Parse(stream).RootElement.Clone();
    }

    private void WriteTo(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", Type);
        if (Description != null)
            writer.WriteString("description", Description);
        if (Properties != null)
        {
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            foreach (var (k, v) in Properties)
            {
                writer.WritePropertyName(k);
                v.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        if (Required != null && Required.Count > 0)
        {
            writer.WritePropertyName("required");
            writer.WriteStartArray();
            foreach (var r in Required)
                writer.WriteStringValue(r);
            writer.WriteEndArray();
        }
        if (Items != null)
        {
            writer.WritePropertyName("items");
            Items.WriteTo(writer);
        }
        if (Enum != null && Enum.Count > 0)
        {
            writer.WritePropertyName("enum");
            writer.WriteStartArray();
            foreach (var e in Enum)
            {
                if (e == null) writer.WriteNullValue();
                else if (e is string s) writer.WriteStringValue(s);
                else if (e is int i) writer.WriteNumberValue(i);
                else if (e is long l) writer.WriteNumberValue(l);
                else if (e is double d) writer.WriteNumberValue(d);
                else writer.WriteStringValue(e.ToString());
            }
            writer.WriteEndArray();
        }
        if (Minimum.HasValue) writer.WriteNumber("minimum", Minimum.Value);
        if (Maximum.HasValue) writer.WriteNumber("maximum", Maximum.Value);
        if (MinLength.HasValue) writer.WriteNumber("minLength", MinLength.Value);
        if (MaxLength.HasValue) writer.WriteNumber("maxLength", MaxLength.Value);
        if (Pattern != null) writer.WriteString("pattern", Pattern);
        writer.WriteEndObject();
    }
}
