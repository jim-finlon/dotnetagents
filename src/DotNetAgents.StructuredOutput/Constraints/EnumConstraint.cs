using DotNetAgents.StructuredOutput;

namespace DotNetAgents.StructuredOutput.Constraints;

/// <summary>Constraint that restricts value to an enum.</summary>
public sealed class EnumConstraint<T> : IConstraint<T> where T : struct, Enum
{
    /// <inheritdoc />
    public string? Validate(T value) => Enum.IsDefined(value) ? null : $"Value {value} is not defined for {typeof(T).Name}.";

    /// <inheritdoc />
    public JsonSchemaDefinition? GetSchemaFragment() =>
        new JsonSchemaDefinition
        {
            Type = "string",
            Enum = Enum.GetNames(typeof(T)).Cast<object>().ToList()
        };
}
