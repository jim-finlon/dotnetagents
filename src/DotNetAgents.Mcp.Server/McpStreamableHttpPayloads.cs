using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

internal static class McpStreamableHttpPayloads
{
    public static object BuildOAuthProbePayload(string serviceName, string path)
    {
        var guidance = "This compatibility endpoint does not provide interactive OAuth. Configure X-Api-Key from CredentialsAgent or the local MCP client environment, then retry POST /mcp.";
        var suggestedNextSteps = new[] { "get_instructions", "retry_with_x_api_key" };
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["path"] = path,
            ["expectedHeader"] = "X-Api-Key",
            ["compatibilityMode"] = "cursor_oauth_probe"
        };

        return new
        {
            success = false,
            error = "API key required for MCP Streamable HTTP.",
            errorCode = "UNAUTHORIZED",
            guidance,
            suggestedNextSteps,
            metadata,
            remediation = BuildRemediation(
                "auth",
                serviceName,
                "oauth_probe",
                "UNAUTHORIZED",
                "X-Api-Key",
                guidance,
                suggestedNextSteps,
                metadata)
        };
    }

    public static object BuildCorsForbiddenPayload(string serviceName)
    {
        var guidance = "Retry from an allowed MCP client origin or remove the unexpected Origin header for trusted LAN/server-side calls.";
        var suggestedNextSteps = new[] { "retry_from_allowed_origin", "inspect_mcp_client_origin" };
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["policy"] = "cursor_mcp_cors",
            ["allowedOrigins"] = "localhost,127.0.0.1,::1,cursor.com"
        };

        return new
        {
            success = false,
            error = "Origin is not allowed for MCP Streamable HTTP.",
            errorCode = "FORBIDDEN",
            guidance,
            suggestedNextSteps,
            metadata,
            remediation = BuildRemediation(
                "permission",
                serviceName,
                "mcp_cors",
                "FORBIDDEN",
                "allowed_origin",
                guidance,
                suggestedNextSteps,
                metadata)
        };
    }

    public static JsonNode BuildJsonRpcRemediationData(
        string serviceName,
        string? toolName,
        int jsonRpcCode,
        string message,
        JsonSerializerOptions jsonOptions)
    {
        var errorCode = jsonRpcCode switch
        {
            -32700 => "PARSE_ERROR",
            -32600 => "INVALID_REQUEST",
            -32601 => "METHOD_NOT_FOUND",
            -32602 => "INVALID_PARAMS",
            _ => "JSON_RPC_ERROR"
        };
        var remediationKind = errorCode switch
        {
            "PARSE_ERROR" or "INVALID_REQUEST" or "INVALID_PARAMS" => "validation",
            "METHOD_NOT_FOUND" => "routing",
            _ => "workflow"
        };
        var guidance = errorCode switch
        {
            "PARSE_ERROR" => "Send a valid JSON-RPC 2.0 request body.",
            "INVALID_REQUEST" => "Include jsonrpc='2.0', id, method, and valid params for non-notification calls.",
            "INVALID_PARAMS" => "Correct the JSON-RPC params shape for the requested MCP method.",
            "METHOD_NOT_FOUND" => "Use initialize, ping, tools/list, or tools/call.",
            _ => "Inspect the JSON-RPC request and retry with the documented MCP method shape."
        };
        var suggestedNextSteps = errorCode == "METHOD_NOT_FOUND"
            ? new[] { "tools/list", "initialize" }
            : new[] { "fix_request_shape", "retry" };
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["jsonRpcCode"] = jsonRpcCode.ToString(CultureInfo.InvariantCulture),
            ["message"] = message
        };

        return JsonSerializer.SerializeToNode(new
        {
            errorCode,
            remediation = BuildRemediation(remediationKind, serviceName, toolName, errorCode, null, guidance, suggestedNextSteps, metadata)
        }, jsonOptions)!;
    }

    public static McpRemediation BuildRemediation(
        string remediationKind,
        string serviceName,
        string? toolName,
        string errorCode,
        string? failedCapability,
        string guidance,
        IReadOnlyList<string> suggestedNextSteps,
        Dictionary<string, string> metadata)
        => new()
        {
            RemediationKind = remediationKind,
            ServiceName = serviceName,
            ToolName = toolName,
            ErrorCode = errorCode,
            FailedCapability = failedCapability,
            Guidance = guidance,
            SuggestedNextSteps = suggestedNextSteps,
            Metadata = metadata
        };
}
