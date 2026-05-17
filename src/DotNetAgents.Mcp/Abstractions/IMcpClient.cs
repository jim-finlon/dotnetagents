using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Abstractions;

/// <summary>
/// Interface for MCP (Model Context Protocol) client.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// Gets the service name this client connects to.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Lists all available tools from the MCP service.
    /// </summary>
    /// <param name="request">Optional request parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The list of available tools.</returns>
    Task<McpListToolsResponse> ListToolsAsync(
        McpListToolsRequest? request = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a specific tool on the MCP service.
    /// </summary>
    /// <param name="request">The tool call request.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The tool call response.</returns>
    Task<McpToolCallResponse> CallToolAsync(
        McpToolCallRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of the MCP service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The health status of the service.</returns>
    Task<McpServiceHealth> GetHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific tool definition by name.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The tool definition, or null if not found.</returns>
    Task<McpToolDefinition?> GetToolAsync(
        string toolName,
        CancellationToken cancellationToken = default);
}
