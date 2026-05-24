// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

using DotNetAgents.Abstractions.OutputParsers;

namespace DotNetAgents.Core.OutputParsers;

/// <summary>
/// Output parser that parses JSON output from LLMs.
/// </summary>
/// <typeparam name="T">The type to deserialize the JSON into.</typeparam>
public class JsonOutputParser<T> : IOutputParser<T>
{
    private readonly JsonSerializerOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonOutputParser{T}"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options.</param>
    public JsonOutputParser(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc/>
    public Task<T> ParseAsync(string output, CancellationToken cancellationToken = default)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        try
        {
            // Try to extract JSON from markdown code blocks if present
            var jsonText = ExtractJsonFromMarkdown(output);
            var result = JsonSerializer.Deserialize<T>(jsonText, _options);

            if (result == null)
            {
                throw new ParsingException(
                    $"Failed to deserialize JSON. Result is null.",
                    output);
            }

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            throw new DotNetAgents.Core.OutputParsers.ParsingException(
                $"Failed to parse JSON output: {ex.Message}",
                output,
                ex);
        }
    }

    /// <inheritdoc/>
    public string GetFormatInstructions()
    {
        return "Respond with valid JSON only. Do not include any text before or after the JSON.";
    }

    private static string ExtractJsonFromMarkdown(string text)
    {
        // Check for JSON in markdown code blocks
        var jsonBlockPattern = @"```(?:json)?\s*(\{.*?\}|\[.*?\])\s*```";
        var match = System.Text.RegularExpressions.Regex.Match(text, jsonBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        // Check for JSON wrapped in triple backticks without language tag
        var simpleBlockPattern = @"```\s*(\{.*?\}|\[.*?\])\s*```";
        match = System.Text.RegularExpressions.Regex.Match(text, simpleBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        // Return as-is if no markdown code blocks found
        return text.Trim();
    }
}
