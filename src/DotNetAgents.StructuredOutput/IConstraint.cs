namespace DotNetAgents.StructuredOutput;

/// <summary>
/// Constraint for validating or describing structured output of type <typeparamref name="T"/>.
/// </summary>
public interface IConstraint<in T>
{
    /// <summary>Validates the value. Returns null if valid, or an error message.</summary>
    string? Validate(T value);

    /// <summary>Optional JSON Schema fragment for this constraint (for provider hints).</summary>
    JsonSchemaDefinition? GetSchemaFragment();
}
