using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

/// <summary>
/// Provides MCP tools to the server: list of tool definitions and invocation by name.
/// Implement this in an agent (e.g. TimeManagement) and register with MapMcpEndpoints.
/// </summary>
public interface IMcpToolProvider
{
    /// <summary>
    /// Returns all tools this provider exposes. Tag with <paramref name="serviceName"/> so the client can attribute calls.
    /// </summary>
    Task<IReadOnlyList<McpToolDefinition>> GetToolsAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the tool by name with the given arguments. Returns MCP-shaped response (Success, Result, Summary, Guidance, etc.).
    /// </summary>
    Task<McpToolCallResponse> CallToolAsync(string toolName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken = default);
}
