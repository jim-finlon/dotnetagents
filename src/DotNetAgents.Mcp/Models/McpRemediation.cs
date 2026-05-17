namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Machine-readable remediation details for failed MCP tool calls.
/// </summary>
public sealed record McpRemediation
{
    /// <summary>Stable remediation category, such as validation, auth, permission, safety, or workflow.</summary>
    public string? RemediationKind { get; init; }

    /// <summary>Name of the MCP service that produced the failure, when known.</summary>
    public string? ServiceName { get; init; }

    /// <summary>Name of the MCP tool that produced the failure, when known.</summary>
    public string? ToolName { get; init; }

    /// <summary>Programmatic error code mirrored from the response envelope.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Argument name that should be corrected before retry, when applicable.</summary>
    public string? InvalidArgument { get; init; }

    /// <summary>Capability, scope, role, or permission that blocked the call, when applicable.</summary>
    public string? FailedCapability { get; init; }

    /// <summary>Human- and model-readable recovery guidance that does not contain secrets.</summary>
    public string? Guidance { get; init; }

    /// <summary>Safe follow-up tool names or operator actions.</summary>
    public IReadOnlyList<string> SuggestedNextSteps { get; init; } = Array.Empty<string>();

    /// <summary>Additional non-secret context for clients that need service-specific details.</summary>
    public Dictionary<string, string> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional service-native remediation payload for clients that need a richer contract than Metadata.</summary>
    public object? Payload { get; init; }
}
