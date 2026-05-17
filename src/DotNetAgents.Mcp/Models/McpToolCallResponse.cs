namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Represents the response from an MCP tool call.
/// Per Anthropic's MCP vision: provide context for the consumer (summary, guidance, next steps), not just raw data in Result.
/// </summary>
public record McpToolCallResponse
{
    /// <summary>
    /// Gets a value indicating whether the tool call was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the result of the tool call (if successful). Structured data for the consumer.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Optional. Brief human- and model-readable summary of what the result means (context, not just data).
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Optional. Guidance on how to interpret or use the result in relation to the user's request.
    /// </summary>
    public string? Guidance { get; init; }

    /// <summary>
    /// Optional. Suggested follow-up tools or actions (e.g. "get_scan_status", "list_scan_results").
    /// </summary>
    public IReadOnlyList<string>? SuggestedNextSteps { get; init; }

    /// <summary>
    /// Optional. Machine-readable remediation details for failed calls. Providers should avoid raw secrets here.
    /// </summary>
    public McpRemediation? Remediation { get; init; }

    /// <summary>
    /// Gets the error message (if the call failed).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the error code (if the call failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets the duration of the tool call in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Gets additional metadata about the tool call.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}
