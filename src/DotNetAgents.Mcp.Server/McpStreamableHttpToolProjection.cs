// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

internal static class McpStreamableHttpToolProjection
{
    public static object JsonElementToArg(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString()!,
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null!,
        JsonValueKind.Object => el,
        JsonValueKind.Array => el,
        _ => el.GetRawText()
    };

    public static JsonObject BuildMcpToolJson(McpToolDefinition t, JsonSerializerOptions jsonOptions)
    {
        var input = BuildInputSchemaObject(t.InputSchema);
        var tool = new JsonObject
        {
            ["name"] = t.Name,
            ["description"] = t.Description,
            ["inputSchema"] = JsonSerializer.SerializeToNode(input, jsonOptions)
        };
        return tool;
    }

    public static CallToolResultDto McpToCallToolResult(McpToolCallResponse response, JsonSerializerOptions jsonOptions)
    {
        var text = JsonSerializer.Serialize(response, jsonOptions);
        JsonNode? structuredContent = response.Result switch
        {
            null when !response.Success => JsonSerializer.SerializeToNode(new
            {
                response.Success,
                response.Error,
                response.ErrorCode,
                response.Guidance,
                response.SuggestedNextSteps,
                response.Metadata,
                response.Remediation
            }, jsonOptions),
            null => null,
            JsonElement je => JsonSerializer.SerializeToNode(je, jsonOptions),
            _ => JsonSerializer.SerializeToNode(response.Result, jsonOptions)
        };

        structuredContent = EnsureStructuredContentIsObject(structuredContent);

        return new CallToolResultDto
        {
            Content =
            [
                new ContentBlockDto { Type = "text", Text = text }
            ],
            StructuredContent = structuredContent,
            IsError = !response.Success
        };
    }

    private static Dictionary<string, object?> BuildInputSchemaObject(McpToolInputSchema schema)
    {
        var props = new Dictionary<string, object?>();
        foreach (var kv in schema.Properties)
        {
            props[kv.Key] = BuildPropertyJson(kv.Value);
        }

        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = props,
            ["required"] = schema.Required
        };
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

    private static JsonNode? EnsureStructuredContentIsObject(JsonNode? node)
    {
        if (node is null)
            return null;
        if (node is JsonObject)
            return node;
        return new JsonObject { ["result"] = node };
    }
}

internal sealed class CallToolResultDto
{
    public ContentBlockDto[] Content { get; set; } = [];
    public JsonNode? StructuredContent { get; set; }
    public bool? IsError { get; set; }
}

internal sealed class ContentBlockDto
{
    public string Type { get; set; } = "text";
    public string? Text { get; set; }
}
