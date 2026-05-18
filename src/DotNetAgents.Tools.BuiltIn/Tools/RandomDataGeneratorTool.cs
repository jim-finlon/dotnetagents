using System.Text.Json;
using DotNetAgents.Abstractions.Tools;

using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for generating random data (numbers, strings, UUIDs, etc.).
/// </summary>
public class RandomDataGeneratorTool : ITool
{
    private static readonly JsonElement _inputSchema;

    static RandomDataGeneratorTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""type"": {
                    ""type"": ""string"",
                    ""description"": ""Type of random data: 'integer', 'double', 'string', 'uuid', 'boolean', 'guid'"",
                    ""enum"": [""integer"", ""double"", ""string"", ""uuid"", ""boolean"", ""guid""]
                },
                ""min"": {
                    ""type"": ""integer"",
                    ""description"": ""Minimum value for integer/double (default: 0 for integer, 0.0 for double)""
                },
                ""max"": {
                    ""type"": ""integer"",
                    ""description"": ""Maximum value for integer/double (default: 100 for integer, 1.0 for double)""
                },
                ""length"": {
                    ""type"": ""integer"",
                    ""description"": ""Length for random string (default: 10)""
                },
                ""charset"": {
                    ""type"": ""string"",
                    ""description"": ""Character set for random string: 'alphanumeric', 'alphabetic', 'numeric', 'hex'. Default: 'alphanumeric'""
                }
            },
            ""required"": [""type""]
        }");
    }

    /// <inheritdoc/>
    public string Name => "random_data_generator";

    /// <inheritdoc/>
    public string Description => "Generates random data including integers, doubles, strings, UUIDs/GUIDs, and booleans. Useful for testing and generating sample data.";

    /// <inheritdoc/>
    public JsonElement InputSchema => _inputSchema;

    /// <inheritdoc/>
    public Task<ToolResult> ExecuteAsync(
        object input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parameters = ParseInput(input);
        if (!parameters.TryGetValue("type", out var typeObj) || typeObj is not string type)
        {
            return Task.FromResult(ToolResult.Failure("Missing or invalid 'type' parameter."));
        }

        try
        {
            var result = type.ToLowerInvariant() switch
            {
                "integer" => GenerateInteger(parameters),
                "double" => GenerateDouble(parameters),
                "string" => GenerateString(parameters),
                "uuid" or "guid" => Guid.NewGuid().ToString(),
                "boolean" => Random.Shared.Next(0, 2) == 1 ? "true" : "false",
                _ => throw new ArgumentException($"Unknown type: {type}")
            };

            return Task.FromResult(ToolResult.Success(
                result,
                new Dictionary<string, object>
                {
                    ["type"] = type
                }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failure(
                $"Random data generation failed: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["type"] = type
                }));
        }
    }

    private static string GenerateInteger(IDictionary<string, object> parameters)
    {
        var min = 0;
        var max = 100;

        if (parameters.TryGetValue("min", out var minObj))
        {
            min = minObj is int minInt ? minInt :
                  minObj is JsonElement je && je.ValueKind == JsonValueKind.Number ? je.GetInt32() : 0;
        }

        if (parameters.TryGetValue("max", out var maxObj))
        {
            max = maxObj is int maxInt ? maxInt :
                  maxObj is JsonElement je && je.ValueKind == JsonValueKind.Number ? je.GetInt32() : 100;
        }

        if (min >= max)
        {
            throw new ArgumentException("Min must be less than max.");
        }

        return Random.Shared.Next(min, max).ToString();
    }

    private static string GenerateDouble(IDictionary<string, object> parameters)
    {
        var min = 0.0;
        var max = 1.0;

        if (parameters.TryGetValue("min", out var minObj))
        {
            min = minObj is double minDouble ? minDouble :
                  minObj is int minInt ? minInt :
                  minObj is JsonElement je && je.ValueKind == JsonValueKind.Number ? je.GetDouble() : 0.0;
        }

        if (parameters.TryGetValue("max", out var maxObj))
        {
            max = maxObj is double maxDouble ? maxDouble :
                  maxObj is int maxInt ? maxInt :
                  maxObj is JsonElement je && je.ValueKind == JsonValueKind.Number ? je.GetDouble() : 1.0;
        }

        if (min >= max)
        {
            throw new ArgumentException("Min must be less than max.");
        }

        var value = Random.Shared.NextDouble() * (max - min) + min;
        return value.ToString("G15");
    }

    private static string GenerateString(IDictionary<string, object> parameters)
    {
        var length = 10;
        if (parameters.TryGetValue("length", out var lengthObj))
        {
            length = lengthObj is int lengthInt ? lengthInt :
                     lengthObj is JsonElement je && je.ValueKind == JsonValueKind.Number ? je.GetInt32() : 10;
        }

        if (length <= 0 || length > 10000)
        {
            throw new ArgumentException("Length must be between 1 and 10000.");
        }

        var charset = "alphanumeric";
        if (parameters.TryGetValue("charset", out var charsetObj) && charsetObj is string charsetStr)
        {
            charset = charsetStr.ToLowerInvariant();
        }

        var chars = charset switch
        {
            "alphanumeric" => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
            "alphabetic" => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
            "numeric" => "0123456789",
            "hex" => "0123456789abcdef",
            _ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        };

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[Random.Shared.Next(chars.Length)];
        }

        return new string(result);
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
