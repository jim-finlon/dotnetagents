using DotNetAgents.Abstractions.OutputParsers;

namespace DotNetAgents.Core.OutputParsers;

/// <summary>
/// Output parser that parses comma-separated lists.
/// </summary>
public class CommaSeparatedListOutputParser : IOutputParser<IReadOnlyList<string>>
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ParseAsync(string output, CancellationToken cancellationToken = default)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        var items = output
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<string>>(items);
    }

    /// <inheritdoc/>
    public string GetFormatInstructions()
    {
        return "Respond with a comma-separated list of items. Do not include any other text.";
    }
}
