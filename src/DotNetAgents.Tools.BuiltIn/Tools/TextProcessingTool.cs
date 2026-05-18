using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for text processing operations including regex, encoding, and string manipulation.
/// </summary>
public class TextProcessingTool : ITool
{
    private static readonly JsonElement _inputSchema;

    static TextProcessingTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""operation"": {
                    ""type"": ""string"",
                    ""description"": ""Operation to perform: 'regex_match', 'regex_replace', 'regex_find_all', 'base64_encode', 'base64_decode', 'url_encode', 'url_decode', 'trim', 'upper', 'lower', 'split', 'join', 'substring', 'replace'"",
                    ""enum"": [""regex_match"", ""regex_replace"", ""regex_find_all"", ""base64_encode"", ""base64_decode"", ""url_encode"", ""url_decode"", ""trim"", ""upper"", ""lower"", ""split"", ""join"", ""substring"", ""replace""]
                },
                ""text"": {
                    ""type"": ""string"",
                    ""description"": ""The input text to process""
                },
                ""pattern"": {
                    ""type"": ""string"",
                    ""description"": ""Regex pattern (for regex operations)""
                },
                ""replacement"": {
                    ""type"": ""string"",
                    ""description"": ""Replacement string (for replace operations)""
                },
                ""separator"": {
                    ""type"": ""string"",
                    ""description"": ""Separator for split/join operations""
                },
                ""start"": {
                    ""type"": ""integer"",
                    ""description"": ""Start index for substring operation""
                },
                ""length"": {
                    ""type"": ""integer"",
                    ""description"": ""Length for substring operation""
                }
            },
            ""required"": [""operation"", ""text""]
        }");
    }

    /// <inheritdoc/>
    public string Name => "text_processing";

    /// <inheritdoc/>
    public string Description => "Performs text processing operations including regex matching/replacement, encoding/decoding (Base64, URL), and string manipulation (trim, case conversion, split, join, substring, replace).";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("operation", out var operationObj) || operationObj is not string operation)
        {
            return Task.FromResult(ToolResult.Failure("Missing or invalid 'operation' parameter."));
        }

        if (!parameters.TryGetValue("text", out var textObj) || textObj is not string text)
        {
            return Task.FromResult(ToolResult.Failure("Missing or invalid 'text' parameter."));
        }

        try
        {
            return operation.ToLowerInvariant() switch
            {
                "regex_match" => Task.FromResult(RegexMatch(parameters, text)),
                "regex_replace" => Task.FromResult(RegexReplace(parameters, text)),
                "regex_find_all" => Task.FromResult(RegexFindAll(parameters, text)),
                "base64_encode" => Task.FromResult(Base64Encode(text)),
                "base64_decode" => Task.FromResult(Base64Decode(text)),
                "url_encode" => Task.FromResult(UrlEncode(text)),
                "url_decode" => Task.FromResult(UrlDecode(text)),
                "trim" => Task.FromResult(Trim(text)),
                "upper" => Task.FromResult(Upper(text)),
                "lower" => Task.FromResult(Lower(text)),
                "split" => Task.FromResult(Split(parameters, text)),
                "join" => Task.FromResult(Join(parameters, text)),
                "substring" => Task.FromResult(Substring(parameters, text)),
                "replace" => Task.FromResult(Replace(parameters, text)),
                _ => Task.FromResult(ToolResult.Failure($"Unknown operation: {operation}"))
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failure(
                $"Operation failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["operation"] = operation
                }));
        }
    }

    private static ToolResult RegexMatch(IDictionary<string, object> parameters, string text)
    {
        if (!parameters.TryGetValue("pattern", out var patternObj) || patternObj is not string pattern)
        {
            return ToolResult.Failure("Missing 'pattern' parameter for regex_match operation.");
        }

        try
        {
            var match = Regex.Match(text, pattern);
            var result = new Dictionary<string, object>
            {
                ["matched"] = match.Success,
                ["value"] = match.Success ? match.Value : null,
                ["groups"] = match.Groups.Cast<Group>().Select(g => new Dictionary<string, object>
                {
                    ["value"] = g.Value,
                    ["index"] = g.Index,
                    ["length"] = g.Length
                }).ToList()
            };

            return ToolResult.Success(
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["matched"] = match.Success
                });
        }
        catch (ArgumentException ex)
        {
            return ToolResult.Failure($"Invalid regex pattern: {ex.Message}");
        }
    }

    private static ToolResult RegexReplace(IDictionary<string, object> parameters, string text)
    {
        if (!parameters.TryGetValue("pattern", out var patternObj) || patternObj is not string pattern)
        {
            return ToolResult.Failure("Missing 'pattern' parameter for regex_replace operation.");
        }

        var replacement = parameters.TryGetValue("replacement", out var replObj) && replObj is string repl ? repl : "";

        try
        {
            var result = Regex.Replace(text, pattern, replacement);
            return ToolResult.Success(
                result,
                new Dictionary<string, object>
                {
                    ["original_length"] = text.Length,
                    ["result_length"] = result.Length
                });
        }
        catch (ArgumentException ex)
        {
            return ToolResult.Failure($"Invalid regex pattern: {ex.Message}");
        }
    }

    private static ToolResult RegexFindAll(IDictionary<string, object> parameters, string text)
    {
        if (!parameters.TryGetValue("pattern", out var patternObj) || patternObj is not string pattern)
        {
            return ToolResult.Failure("Missing 'pattern' parameter for regex_find_all operation.");
        }

        try
        {
            var matches = Regex.Matches(text, pattern);
            var results = matches.Cast<Match>().Select(m => new Dictionary<string, object>
            {
                ["value"] = m.Value,
                ["index"] = m.Index,
                ["length"] = m.Length
            }).ToList();

            return ToolResult.Success(
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["match_count"] = results.Count
                });
        }
        catch (ArgumentException ex)
        {
            return ToolResult.Failure($"Invalid regex pattern: {ex.Message}");
        }
    }

    private static ToolResult Base64Encode(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var encoded = Convert.ToBase64String(bytes);
        return ToolResult.Success(encoded);
    }

    private static ToolResult Base64Decode(string text)
    {
        try
        {
            var bytes = Convert.FromBase64String(text);
            var decoded = Encoding.UTF8.GetString(bytes);
            return ToolResult.Success(decoded);
        }
        catch (FormatException ex)
        {
            return ToolResult.Failure($"Invalid Base64 string: {ex.Message}");
        }
    }

    private static ToolResult UrlEncode(string text)
    {
        var encoded = Uri.EscapeDataString(text);
        return ToolResult.Success(encoded);
    }

    private static ToolResult UrlDecode(string text)
    {
        try
        {
            var decoded = Uri.UnescapeDataString(text);
            return ToolResult.Success(decoded);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"Invalid URL-encoded string: {ex.Message}");
        }
    }

    private static ToolResult Trim(string text)
    {
        return ToolResult.Success(text.Trim());
    }

    private static ToolResult Upper(string text)
    {
        return ToolResult.Success(text.ToUpperInvariant());
    }

    private static ToolResult Lower(string text)
    {
        return ToolResult.Success(text.ToLowerInvariant());
    }

    private static ToolResult Split(IDictionary<string, object> parameters, string text)
    {
        var separator = parameters.TryGetValue("separator", out var sepObj) && sepObj is string sep ? sep : ",";
        var parts = text.Split(new[] { separator }, StringSplitOptions.None);
        return ToolResult.Success(
            JsonSerializer.Serialize(parts, new JsonSerializerOptions { WriteIndented = true }),
            new Dictionary<string, object>
            {
                ["part_count"] = parts.Length
            });
    }

    private static ToolResult Join(IDictionary<string, object> parameters, string text)
    {
        var separator = parameters.TryGetValue("separator", out var sepObj) && sepObj is string sep ? sep : ",";

        // Try to parse text as JSON array
        try
        {
            var parts = JsonSerializer.Deserialize<string[]>(text) ?? new[] { text };
            var joined = string.Join(separator, parts);
            return ToolResult.Success(joined);
        }
        catch
        {
            // If not JSON, treat as single string
            return ToolResult.Success(text);
        }
    }

    private static ToolResult Substring(IDictionary<string, object> parameters, string text)
    {
        if (!parameters.TryGetValue("start", out var startObj))
        {
            return ToolResult.Failure("Missing 'start' parameter for substring operation.");
        }

        var start = startObj is int startInt ? startInt :
                   startObj is JsonElement startJe && startJe.ValueKind == JsonValueKind.Number ? startJe.GetInt32() : 0;

        if (start < 0 || start >= text.Length)
        {
            return ToolResult.Failure($"Start index {start} is out of range (0-{text.Length - 1})");
        }

        if (parameters.TryGetValue("length", out var lengthObj))
        {
            var length = lengthObj is int lengthInt ? lengthInt :
                        lengthObj is JsonElement lengthJe && lengthJe.ValueKind == JsonValueKind.Number ? lengthJe.GetInt32() : text.Length - start;

            if (length < 0 || start + length > text.Length)
            {
                return ToolResult.Failure($"Length {length} is invalid for start index {start}");
            }

            return ToolResult.Success(text.Substring(start, length));
        }

        return ToolResult.Success(text.Substring(start));
    }

    private static ToolResult Replace(IDictionary<string, object> parameters, string text)
    {
        if (!parameters.TryGetValue("pattern", out var patternObj) || patternObj is not string pattern)
        {
            return ToolResult.Failure("Missing 'pattern' parameter for replace operation.");
        }

        var replacement = parameters.TryGetValue("replacement", out var replObj) && replObj is string repl ? repl : "";
        var result = text.Replace(pattern, replacement, StringComparison.Ordinal);

        return ToolResult.Success(
            result,
            new Dictionary<string, object>
            {
                ["replacements"] = (text.Length - result.Length + replacement.Length * (text.Split(pattern).Length - 1)) / pattern.Length
            });
    }

    private static IDictionary<string, object> ParseInput(object input)
    {
        if (input is JsonElement jsonElement)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in jsonElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString()
                };
            }
            return dict;
        }

        if (input is IDictionary<string, object> dictInput)
        {
            return dictInput;
        }

        throw new ArgumentException("Input must be JsonElement or IDictionary<string, object>", nameof(input));
    }
}
