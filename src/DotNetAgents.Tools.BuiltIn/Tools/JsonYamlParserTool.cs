using System.Text.Json;
using System.Text.RegularExpressions;
using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Tools;


namespace DotNetAgents.Tools.BuiltIn;

/// <summary>
/// A tool for parsing and manipulating JSON and YAML data.
/// </summary>
public class JsonYamlParserTool : ITool
{
    private static readonly JsonElement _inputSchema;

    static JsonYamlParserTool()
    {
        _inputSchema = JsonSerializer.Deserialize<JsonElement>(@"{
            ""type"": ""object"",
            ""properties"": {
                ""operation"": {
                    ""type"": ""string"",
                    ""description"": ""Operation to perform: 'parse', 'stringify', 'extract', 'validate', 'yaml_to_json', 'json_to_yaml'"",
                    ""enum"": [""parse"", ""stringify"", ""extract"", ""validate"", ""yaml_to_json"", ""json_to_yaml""]
                },
                ""data"": {
                    ""type"": ""string"",
                    ""description"": ""The JSON or YAML data to process""
                },
                ""path"": {
                    ""type"": ""string"",
                    ""description"": ""JSONPath expression for 'extract' operation (e.g., '$.users[0].name')""
                },
                ""format"": {
                    ""type"": ""string"",
                    ""description"": ""Format for stringify: 'json' or 'yaml'. Default: 'json'"",
                    ""enum"": [""json"", ""yaml""]
                }
            },
            ""required"": [""operation"", ""data""]
        }");
    }

    /// <inheritdoc/>
    public string Name => "json_yaml_parser";

    /// <inheritdoc/>
    public string Description => "Parses, validates, and manipulates JSON and YAML data. Supports parsing, stringifying, extracting values using JSONPath, and converting between JSON and YAML formats.";

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

        if (!parameters.TryGetValue("data", out var dataObj) || dataObj is not string data)
        {
            return Task.FromResult(ToolResult.Failure("Missing or invalid 'data' parameter."));
        }

        try
        {
            return operation.ToLowerInvariant() switch
            {
                "parse" => Task.FromResult(ParseJson(data)),
                "stringify" => Task.FromResult(Stringify(parameters, data)),
                "extract" => Task.FromResult(Extract(parameters, data)),
                "validate" => Task.FromResult(Validate(data)),
                "yaml_to_json" => Task.FromResult(YamlToJson(data)),
                "json_to_yaml" => Task.FromResult(JsonToYaml(data)),
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

    private static ToolResult ParseJson(string data)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(data);
            return ToolResult.Success(
                JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["valid"] = true,
                    ["root_type"] = jsonDoc.RootElement.ValueKind.ToString()
                });
        }
        catch (JsonException ex)
        {
            return ToolResult.Failure($"Invalid JSON: {ex.Message}");
        }
    }

    private static ToolResult Stringify(IDictionary<string, object> parameters, string data)
    {
        try
        {
            var format = "json";
            if (parameters.TryGetValue("format", out var formatObj) && formatObj is string formatStr)
            {
                format = formatStr.ToLowerInvariant();
            }

            // Parse first to validate
            var jsonDoc = JsonDocument.Parse(data);

            if (format == "yaml")
            {
                var yaml = JsonToYaml(data);
                return yaml;
            }

            return ToolResult.Success(
                JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["format"] = format
                });
        }
        catch (JsonException ex)
        {
            return ToolResult.Failure($"Invalid JSON: {ex.Message}");
        }
    }

    private static ToolResult Extract(IDictionary<string, object> parameters, string data)
    {
        if (!parameters.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return ToolResult.Failure("Missing 'path' parameter for extract operation.");
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(data);
            var result = EvaluateJsonPath(jsonDoc.RootElement, path);

            return ToolResult.Success(
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
                new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["found"] = result != null
                });
        }
        catch (JsonException ex)
        {
            return ToolResult.Failure($"Invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"JSONPath evaluation failed: {ex.Message}");
        }
    }

    private static ToolResult Validate(string data)
    {
        try
        {
            JsonDocument.Parse(data);
            return ToolResult.Success(
                "Valid JSON",
                new Dictionary<string, object>
                {
                    ["valid"] = true
                });
        }
        catch (JsonException ex)
        {
            return ToolResult.Success(
                $"Invalid JSON: {ex.Message}",
                new Dictionary<string, object>
                {
                    ["valid"] = false,
                    ["error"] = ex.Message
                });
        }
    }

    private static ToolResult YamlToJson(string yamlData)
    {
        // Basic YAML to JSON conversion
        // Note: For production, use a proper YAML library like YamlDotNet
        // This is a simplified implementation
        try
        {
            // Placeholder - would need YamlDotNet or similar
            return ToolResult.Failure(
                "YAML to JSON conversion requires YamlDotNet package. This feature is not yet fully implemented.",
                new Dictionary<string, object>
                {
                    ["note"] = "Install YamlDotNet package to enable YAML support"
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"YAML conversion failed: {ex.Message}");
        }
    }

    private static ToolResult JsonToYaml(string jsonData)
    {
        // Basic JSON to YAML conversion
        // Note: For production, use a proper YAML library like YamlDotNet
        try
        {
            // Placeholder - would need YamlDotNet or similar
            return ToolResult.Failure(
                "JSON to YAML conversion requires YamlDotNet package. This feature is not yet fully implemented.",
                new Dictionary<string, object>
                {
                    ["note"] = "Install YamlDotNet package to enable YAML support"
                });
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"YAML conversion failed: {ex.Message}");
        }
    }

    private static object? EvaluateJsonPath(JsonElement element, string path)
    {
        // Simplified JSONPath evaluation
        // For production, use a library like JsonPath.Net or System.Text.Json.JsonPath
        if (string.IsNullOrWhiteSpace(path) || path == "$")
        {
            return element;
        }

        var parts = path.TrimStart('$').Split('.');
        var current = element;

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            // Handle array access [0]
            var arrayMatch = Regex.Match(part, @"^(\w+)\[(\d+)\]$");
            if (arrayMatch.Success)
            {
                var propertyName = arrayMatch.Groups[1].Value;
                var index = int.Parse(arrayMatch.Groups[2].Value);

                if (current.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
                {
                    current = prop[index];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (current.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => current.ToString()
        };
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
