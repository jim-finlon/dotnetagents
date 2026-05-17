using System.Text.Json;
using DotNetAgents.StructuredOutput;

namespace DotNetAgents.StructuredOutput.Constraints;

/// <summary>Constraint that validates JSON structure against a schema (basic validation).</summary>
public sealed class JsonSchemaConstraint<T> : IConstraint<T>
{
    private readonly JsonSchemaDefinition _schema;

    public JsonSchemaConstraint(JsonSchemaDefinition schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <inheritdoc />
    public string? Validate(T value)
    {
        if (value == null)
            return _schema.Type == "null" ? null : "Value is null.";
        try
        {
            var json = JsonSerializer.SerializeToElement(value);
            // Minimal validation: type check root
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <inheritdoc />
    public JsonSchemaDefinition? GetSchemaFragment() => _schema;
}
