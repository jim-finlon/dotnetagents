using DotNetAgents.StructuredOutput;

namespace DotNetAgents.StructuredOutput.Constraints;

/// <summary>Constraint for numeric range (inclusive).</summary>
public sealed class RangeConstraint : IConstraint<double>
{
    private readonly double _min;
    private readonly double _max;

    public RangeConstraint(double min, double max)
    {
        _min = min;
        _max = max;
    }

    /// <inheritdoc />
    public string? Validate(double value)
    {
        if (value < _min || value > _max)
            return $"Value {value} is not in range [{_min}, {_max}].";
        return null;
    }

    /// <inheritdoc />
    public JsonSchemaDefinition? GetSchemaFragment() => new JsonSchemaDefinition { Type = "number", Minimum = _min, Maximum = _max };
}
