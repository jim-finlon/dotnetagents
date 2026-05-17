using System.Text;
using System.Text.RegularExpressions;

using DotNetAgents.Abstractions.Prompts;

namespace DotNetAgents.Core.Prompts;

/// <summary>
/// Implementation of <see cref="IPromptTemplate"/> that supports variable substitution using {variable} syntax.
/// </summary>
public class PromptTemplate : IPromptTemplate
{
    private static readonly Regex VariablePattern = new(@"\{(\w+)\}", RegexOptions.Compiled);
    private readonly HashSet<string> _variables;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTemplate"/> class.
    /// </summary>
    /// <param name="template">The template string with variables in {variable} format.</param>
    /// <exception cref="ArgumentNullException">Thrown when template is null.</exception>
    public PromptTemplate(string template)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        _variables = ExtractVariables(template);
    }

    /// <inheritdoc/>
    public string Template { get; }

    /// <inheritdoc/>
    public IReadOnlySet<string> Variables => _variables;

    /// <inheritdoc/>
    public Task<string> FormatAsync(
        IDictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        ValidateVariables(variables);

        var result = VariablePattern.Replace(Template, match =>
        {
            var variableName = match.Groups[1].Value;

            // Try exact match first
            if (variables.TryGetValue(variableName, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }

            // Try case-insensitive match
            var matchingKey = variables.Keys.FirstOrDefault(
                k => string.Equals(k, variableName, StringComparison.OrdinalIgnoreCase));
            if (matchingKey != null && variables.TryGetValue(matchingKey, out value))
            {
                return value?.ToString() ?? string.Empty;
            }

            return match.Value; // Keep original if not found (shouldn't happen after validation)
        });

        return Task.FromResult(result);
    }

    private static HashSet<string> ExtractVariables(string template)
    {
        var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = VariablePattern.Matches(template);

        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value);
        }

        return variables;
    }

    private void ValidateVariables(IDictionary<string, object> variables)
    {
        var variableKeys = new HashSet<string>(variables.Keys, StringComparer.OrdinalIgnoreCase);
        var missingVariables = _variables
            .Where(v => !variableKeys.Contains(v))
            .ToList();

        if (missingVariables.Count > 0)
        {
            throw new ArgumentException(
                $"Missing required variables: {string.Join(", ", missingVariables)}",
                nameof(variables));
        }
    }
}
