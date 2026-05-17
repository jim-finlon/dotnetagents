namespace DotNetAgents.Voice.Commands;

/// <summary>
/// Default implementation of <see cref="ICommandTemplate"/>.
/// </summary>
public class CommandTemplate : ICommandTemplate
{
    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    public required string Description { get; init; }

    /// <inheritdoc />
    public required string Template { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <inheritdoc />
    public string Render(Dictionary<string, object> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var result = Template;

        foreach (var (key, value) in values)
        {
            var placeholder = $"{{{key}}}";
            var stringValue = value?.ToString() ?? string.Empty;
            result = result.Replace(placeholder, stringValue);
        }

        // Check for unresolved placeholders
        var unresolved = System.Text.RegularExpressions.Regex.Matches(result, @"\{(\w+)\}")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();

        if (unresolved.Count > 0)
        {
            throw new ArgumentException(
                $"Template has unresolved placeholders: {string.Join(", ", unresolved)}",
                nameof(values));
        }

        return result;
    }
}
