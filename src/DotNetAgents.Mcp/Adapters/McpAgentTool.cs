using System.Text.Json;
using DotNetAgents.Abstractions.Tools;
using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Adapters;

/// <summary>
/// Wraps one <see cref="McpToolDefinition"/> as an <see cref="ITool"/> so an
/// <c>AgentExecutor</c>'s ReAct loop can invoke MCP tools without knowing about MCP.
/// Per RW-5 (story e05c7b1e). Generic primitive — no role knowledge here; per-role allowlist
/// + forbidden-tools deny-list enforcement live in the consuming layer (WorkflowService's
/// <c>IRoleScopedMcpToolProvider</c>).
/// </summary>
internal sealed class McpAgentTool : ITool
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly McpToolDefinition _definition;
    private readonly IMcpClientFactory _clientFactory;
    private readonly Lazy<JsonElement> _inputSchema;

    public McpAgentTool(McpToolDefinition definition, IMcpClientFactory clientFactory)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _inputSchema = new Lazy<JsonElement>(BuildInputSchema);
    }

    public string Name => _definition.Name;

    public string Description => _definition.Description;

    public JsonElement InputSchema => _inputSchema.Value;

    public async Task<ToolResult> ExecuteAsync(object input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(_definition.ServiceName))
        {
            return ToolResult.Failure(
                $"MCP tool '{_definition.Name}' has no ServiceName declared; cannot route the call.");
        }

        IMcpClient client;
        try
        {
            client = _clientFactory.GetClient(_definition.ServiceName!);
        }
        catch (InvalidOperationException ex)
        {
            return ToolResult.Failure(
                $"MCP service '{_definition.ServiceName}' is not registered: {ex.Message}");
        }

        var arguments = NormalizeArguments(input);
        var request = new McpToolCallRequest
        {
            Tool = _definition.Name,
            Arguments = arguments,
        };

        try
        {
            var response = await client.CallToolAsync(request, cancellationToken).ConfigureAwait(false);
            return ProjectResponse(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(
                $"MCP tool '{_definition.Name}' on service '{_definition.ServiceName}' threw: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert any of the shapes an <c>AgentExecutor</c> typically passes (JsonElement,
    /// Dictionary&lt;string, object&gt;, anonymous object) into the
    /// <see cref="McpToolCallRequest.Arguments"/> dictionary the MCP wire protocol expects.
    /// </summary>
    private static Dictionary<string, object> NormalizeArguments(object input)
    {
        return input switch
        {
            Dictionary<string, object> dict => dict,
            JsonElement json when json.ValueKind == JsonValueKind.Object => JsonElementToDict(json),
            string str => JsonStringToDict(str),
            _ => ObjectToDict(input),
        };
    }

    private static Dictionary<string, object> JsonElementToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.Clone();
        }
        return dict;
    }

    private static Dictionary<string, object> JsonStringToDict(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return new Dictionary<string, object>(StringComparer.Ordinal);
        }
        try
        {
            using var doc = JsonDocument.Parse(str);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                ? JsonElementToDict(doc.RootElement)
                : new Dictionary<string, object>(StringComparer.Ordinal) { ["value"] = str };
        }
        catch (JsonException)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal) { ["value"] = str };
        }
    }

    private static Dictionary<string, object> ObjectToDict(object input)
    {
        var json = JsonSerializer.SerializeToElement(input, JsonOptions);
        return json.ValueKind == JsonValueKind.Object
            ? JsonElementToDict(json)
            : new Dictionary<string, object>(StringComparer.Ordinal) { ["value"] = input };
    }

    private static ToolResult ProjectResponse(McpToolCallResponse response)
    {
        if (!response.Success)
        {
            return ToolResult.Failure(
                response.Error ?? "MCP tool call failed without an error message.",
                BuildMetadata(response));
        }

        return ToolResult.Success(response.Result, BuildMetadata(response));
    }

    private static IDictionary<string, object>? BuildMetadata(McpToolCallResponse response)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(response.Summary))
            metadata["mcp.summary"] = response.Summary!;
        if (!string.IsNullOrEmpty(response.Guidance))
            metadata["mcp.guidance"] = response.Guidance!;
        if (response.SuggestedNextSteps is { Count: > 0 } next)
            metadata["mcp.suggested_next_steps"] = next;
        if (response.DurationMs > 0)
            metadata["mcp.duration_ms"] = response.DurationMs;
        if (!string.IsNullOrEmpty(response.ErrorCode))
            metadata["mcp.error_code"] = response.ErrorCode!;
        foreach (var (k, v) in response.Metadata)
        {
            // Don't shadow the well-known keys above with consumer-supplied ones.
            metadata.TryAdd($"mcp.meta.{k}", v);
        }
        return metadata.Count == 0 ? null : metadata;
    }

    private JsonElement BuildInputSchema()
    {
        // McpToolInputSchema is a friendlier object; project it to a JSON-Schema-shaped JsonElement
        // so AgentExecutor receives the same input-schema shape it does for other ITool sources.
        // Story c09513a4: recursive projection emits `items`, nested `properties`, and
        // `required` so object-arrays announce their element shape (parity with the
        // Streamable HTTP `tools/list` projection in McpStreamableHttpExtensions).
        var schema = new Dictionary<string, object?>
        {
            ["type"] = _definition.InputSchema.Type,
            ["properties"] = _definition.InputSchema.Properties.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)BuildPropertyJson(kvp.Value)),
            ["required"] = _definition.InputSchema.Required,
        };
        return JsonSerializer.SerializeToElement(schema, JsonOptions);
    }

    private static Dictionary<string, object?> BuildPropertyJson(McpProperty property)
    {
        var p = new Dictionary<string, object?>
        {
            ["type"] = property.Type
        };
        if (!string.IsNullOrEmpty(property.Description))
            p["description"] = property.Description;
        if (property.Enum is { Count: > 0 })
            p["enum"] = property.Enum;
        if (property.Default is not null)
            p["default"] = property.Default;
        if (property.Items is not null)
            p["items"] = BuildPropertyJson(property.Items);
        if (property.Properties is { Count: > 0 })
        {
            var nested = new Dictionary<string, object?>();
            foreach (var nv in property.Properties)
                nested[nv.Key] = BuildPropertyJson(nv.Value);
            p["properties"] = nested;
        }
        if (property.Required is { Count: > 0 })
            p["required"] = property.Required;
        return p;
    }
}
