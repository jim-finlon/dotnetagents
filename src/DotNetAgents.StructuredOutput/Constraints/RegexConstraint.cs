using System.Text.RegularExpressions;
using DotNetAgents.StructuredOutput;

namespace DotNetAgents.StructuredOutput.Constraints;

/// <summary>Constraint that validates a string against a regex pattern.</summary>
public sealed class RegexConstraint : IConstraint<string>
{
    private readonly Regex _regex;

    public RegexConstraint(string pattern)
    {
        _regex = new Regex(pattern ?? throw new ArgumentNullException(nameof(pattern)));
    }

    /// <inheritdoc />
    public string? Validate(string? value)
    {
        if (value == null) return "Value is null.";
        return _regex.IsMatch(value) ? null : $"Value does not match pattern: {_regex.ToString()}";
    }

    /// <inheritdoc />
    public JsonSchemaDefinition? GetSchemaFragment() => new JsonSchemaDefinition { Type = "string", Pattern = _regex.ToString() };
}
