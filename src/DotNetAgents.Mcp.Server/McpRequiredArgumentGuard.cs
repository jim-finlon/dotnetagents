using System.Text.Json;
using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

/// <summary>
/// Ensures <c>POST /mcp/tools/call</c> (and streamable <c>tools/call</c>) reject empty or incomplete
/// <c>arguments</c> when the tool's published <see cref="McpToolInputSchema.Required"/> list is non-empty,
/// matching planning_tools / Evaluation Sandbox MCP contract parity (MISSING_ARG + guidance).
/// </summary>
internal static class McpRequiredArgumentGuard
{
    /// <summary>
    /// When <paramref name="definition"/> is non-null and lists required properties, returns a failure
    /// response if any are absent or JSON null; otherwise null (including when <paramref name="definition"/>
    /// is null so unknown tools still reach <see cref="IMcpToolProvider.CallToolAsync"/>).
    /// </summary>
    public static McpToolCallResponse? TryGetMissingArgumentResponse(
        McpToolDefinition? definition,
        string toolName,
        IReadOnlyDictionary<string, object> arguments)
    {
        if (definition is null)
            return null;

        foreach (var propName in definition.InputSchema.Required)
        {
            if (string.IsNullOrWhiteSpace(propName))
                continue;

            if (!arguments.TryGetValue(propName, out var raw) || !IsProvided(raw))
            {
                return new McpToolCallResponse
                {
                    Success = false,
                    Error = $"Missing or invalid required argument '{propName}' for tool '{toolName}'.",
                    ErrorCode = "MISSING_ARG",
                    Guidance =
                        $"Include '{propName}' in arguments per GET /mcp/tools inputSchema.required for this tool. Value constraints (blank strings, formats) are validated when the tool runs.",
                    SuggestedNextSteps = ["get_instructions", "list_tools", toolName],
                    Remediation = new McpRemediation
                    {
                        RemediationKind = "validation",
                        ToolName = toolName,
                        ErrorCode = "MISSING_ARG",
                        InvalidArgument = propName,
                        Guidance =
                            $"Include '{propName}' in arguments per GET /mcp/tools inputSchema.required for this tool. Value constraints (blank strings, formats) are validated when the tool runs.",
                        SuggestedNextSteps = ["get_instructions", "list_tools", toolName],
                        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["invalidArgument"] = propName,
                            ["validationKind"] = "missing_required_argument"
                        }
                    }
                };
            }
        }

        return null;
    }

    private static bool IsProvided(object? value)
    {
        if (value is null)
            return false;

        if (value is JsonElement je)
            return je.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

        return true;
    }
}
