using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Heuristic signals that a tool may be high-impact (preview/confirm, human approval, or extra audit).
/// DNA services may still declare explicit policy in MCP safety verifiers — this only assists conformance checks and docs.
/// </summary>
public static class McpHighImpactToolHeuristics
{
    private static readonly string[] NameTokens =
    [
        "delete", "remove", "destroy", "drop", "wipe", "purge", "rotate", "revoke",
        "deploy", "promote", "rollback", "reboot", "shutdown", "format", "quarantine",
        "merge", "publish", "release", "approve", "billing", "payment"
    ];

    /// <summary>
    /// Returns true when the tool name or description suggests a destructive or production-affecting action.
    /// </summary>
    public static bool LikelyHighImpact(McpToolDefinition tool)
    {
        var name = tool.Name ?? string.Empty;
        var desc = tool.Description ?? string.Empty;
        var combined = $"{name} {desc}".ToLowerInvariant();
        foreach (var token in NameTokens)
        {
            if (combined.Contains(token, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
