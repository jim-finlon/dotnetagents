namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Well-known <see cref="DotNetAgents.Mcp.Models.McpToolCallResponse.ErrorCode"/> values used across DNA MCP services
/// (server defaults, safety verifier, and tool providers). Consumers may define additional codes; these are canonical
/// strings for conformance tests and documentation.
/// </summary>
public static class McpCanonicalErrorCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ToolError = "TOOL_ERROR";
    /// <summary>Story f34c78e1: canonical code returned when POST /mcp/tools/call references a toolName the service does not expose.</summary>
    public const string UnknownTool = "UNKNOWN_TOOL";
    /// <summary>Story f34c78e1: canonical code for required argument absent or blank.</summary>
    public const string MissingArgument = "MISSING_ARG";
}
