using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace DotNetAgents.Mcp.Server;

internal static class McpStreamableHttpJsonRpc
{
    public static IResult Result(JsonElement id, JsonNode? result, JsonSerializerOptions jsonOptions)
    {
        var o = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNode.Parse(id.GetRawText()),
            ["result"] = result ?? new JsonObject()
        };
        return Results.Json(o, jsonOptions, statusCode: StatusCodes.Status200OK);
    }

    public static IResult BadRequest(
        JsonElement? id,
        int code,
        string message,
        string serviceName,
        JsonSerializerOptions jsonOptions)
    {
        var idNode = id.HasValue ? JsonNode.Parse(id.Value.GetRawText()) : null;
        var err = new JsonObject
        {
            ["code"] = code,
            ["message"] = message,
            ["data"] = McpStreamableHttpPayloads.BuildJsonRpcRemediationData(serviceName, null, code, message, jsonOptions)
        };
        var o = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = idNode,
            ["error"] = err
        };
        return Results.Json(o, jsonOptions, MediaTypeNames.Application.Json, StatusCodes.Status400BadRequest);
    }

    public static IResult Error(
        JsonElement id,
        int code,
        string message,
        string serviceName,
        JsonSerializerOptions jsonOptions,
        object? data = null)
    {
        var err = new JsonObject
        {
            ["code"] = code,
            ["message"] = message
        };
        if (data != null)
            err["data"] = JsonSerializer.SerializeToNode(data, jsonOptions);
        else
            err["data"] = McpStreamableHttpPayloads.BuildJsonRpcRemediationData(serviceName, null, code, message, jsonOptions);
        var o = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = JsonNode.Parse(id.GetRawText()),
            ["error"] = err
        };
        return Results.Json(o, jsonOptions, MediaTypeNames.Application.Json, StatusCodes.Status200OK);
    }

    public static JsonElement? GetRequestId(JsonElement root) =>
        root.TryGetProperty("id", out var id) ? id : null;
}
