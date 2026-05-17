using DotNetAgents.StructuredOutput;

namespace DotNetAgents.StructuredOutput.Constraints;

/// <summary>Constraint that validates each element of a list.</summary>
public sealed class ListConstraint<T> : IConstraint<IReadOnlyList<T>>
{
    private readonly IConstraint<T> _elementConstraint;
    private readonly int? _minCount;
    private readonly int? _maxCount;

    public ListConstraint(IConstraint<T> elementConstraint, int? minCount = null, int? maxCount = null)
    {
        _elementConstraint = elementConstraint ?? throw new ArgumentNullException(nameof(elementConstraint));
        _minCount = minCount;
        _maxCount = maxCount;
    }

    /// <inheritdoc />
    public string? Validate(IReadOnlyList<T>? value)
    {
        if (value == null)
            return _minCount is > 0 ? "List is null." : null;
        if (_minCount.HasValue && value.Count < _minCount.Value)
            return $"List has {value.Count} items; minimum is {_minCount.Value}.";
        if (_maxCount.HasValue && value.Count > _maxCount.Value)
            return $"List has {value.Count} items; maximum is {_maxCount.Value}.";
        for (var i = 0; i < value.Count; i++)
        {
            var err = _elementConstraint.Validate(value[i]);
            if (err != null)
                return $"Item[{i}]: {err}";
        }
        return null;
    }

    /// <inheritdoc />
    public JsonSchemaDefinition? GetSchemaFragment()
    {
        var items = _elementConstraint.GetSchemaFragment();
        return new JsonSchemaDefinition
        {
            Type = "array",
            Items = items,
            MinLength = _minCount,
            MaxLength = _maxCount
        };
    }
}
