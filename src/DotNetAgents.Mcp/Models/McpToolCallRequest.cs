using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Represents a request to call an MCP tool. Accepts both the MCP-protocol-spec
/// <c>{"name": "...", "arguments": {...}}</c> envelope and the DNA legacy
/// <c>{"tool": "...", "arguments": {...}}</c> envelope. When both are present, <c>tool</c> wins.
/// </summary>
[JsonConverter(typeof(McpToolCallRequestConverter))]
public record McpToolCallRequest
{
    /// <summary>
    /// Tool name to invoke. Serializes as both <c>tool</c> (legacy) and <c>name</c> (MCP spec) for maximum
    /// client compatibility; deserializes from either.
    /// </summary>
    public string Tool { get; init; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the tool.
    /// </summary>
    /// <remarks>
    /// Voice routing may add <c>jarvisUserMemory</c> (see <see cref="DotNetAgents.Mcp.Routing.IntentParameterKeys.JarvisUserMemory"/>)
    /// when long-term user memory is available; downstream tools should ignore unknown keys they do not use.
    /// </remarks>
    public Dictionary<string, object> Arguments { get; init; } = new();

    /// <summary>
    /// Gets the correlation ID for tracking this call.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the timeout in seconds for this call.
    /// </summary>
    public int? TimeoutSeconds { get; init; }
}

/// <summary>
/// Accepts both the MCP protocol spec (<c>name</c>) and the DNA legacy (<c>tool</c>) JSON envelope shapes
/// on the wire while exposing a single <see cref="McpToolCallRequest.Tool"/> property to .NET callers.
/// Emits both <c>tool</c> and <c>name</c> on serialization so downstream clients on either spec keep working.
/// </summary>
public sealed class McpToolCallRequestConverter : JsonConverter<McpToolCallRequest>
{
    public override McpToolCallRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object for McpToolCallRequest.");

        string? tool = null;
        string? name = null;
        Dictionary<string, object>? arguments = null;
        string? correlationId = null;
        int? timeoutSeconds = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "tool":
                    tool = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;
                case "name":
                    name = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;
                case "arguments":
                    arguments = ReadArgumentsDictionary(ref reader, options);
                    break;
                case "correlationid":
                    correlationId = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;
                case "timeoutseconds":
                    timeoutSeconds = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        var resolved = !string.IsNullOrWhiteSpace(tool) ? tool! : name ?? string.Empty;
        return new McpToolCallRequest
        {
            Tool = resolved,
            Arguments = arguments ?? new Dictionary<string, object>(),
            CorrelationId = correlationId,
            TimeoutSeconds = timeoutSeconds,
        };
    }

    public override void Write(Utf8JsonWriter writer, McpToolCallRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("tool", value.Tool);
        writer.WriteString("name", value.Tool);

        writer.WritePropertyName("arguments");
        JsonSerializer.Serialize(writer, value.Arguments, value.Arguments.GetType(), options);

        if (value.CorrelationId is not null)
            writer.WriteString("correlationId", value.CorrelationId);
        if (value.TimeoutSeconds is int ts)
            writer.WriteNumber("timeoutSeconds", ts);

        writer.WriteEndObject();
    }

    private static Dictionary<string, object> ReadArgumentsDictionary(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new Dictionary<string, object>();
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected an object for 'arguments'.");

        // Delegate to the default binder so downstream tool handlers receive the same
        // JsonElement-based value shape they already expect for Dictionary<string, object>.
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
        return dict ?? new Dictionary<string, object>();
    }
}
