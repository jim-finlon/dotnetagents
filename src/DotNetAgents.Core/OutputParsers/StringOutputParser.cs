// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.OutputParsers;

namespace DotNetAgents.Core.OutputParsers;

/// <summary>
/// Simple output parser that returns the raw string output.
/// </summary>
public class StringOutputParser : IOutputParser<string>
{
    /// <inheritdoc/>
    public Task<string> ParseAsync(string output, CancellationToken cancellationToken = default)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        return Task.FromResult(output);
    }

    /// <inheritdoc/>
    public string GetFormatInstructions() => string.Empty;
}
