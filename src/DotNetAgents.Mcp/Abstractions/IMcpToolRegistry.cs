using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Abstractions;

/// <summary>
/// Interface for tool registry that aggregates tools from all MCP services.
/// </summary>
public interface IMcpToolRegistry
{
    /// <summary>
    /// Gets all available tools from all services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The list of all available tools.</returns>
    Task<List<McpToolDefinition>> GetAllToolsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tools for a specific service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The list of tools for the service.</returns>
    Task<List<McpToolDefinition>> GetToolsForServiceAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a tool by name across all services.
    /// </summary>
    /// <param name="toolName">The name of the tool to find.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The tool definition, or null if not found.</returns>
    Task<McpToolDefinition?> FindToolAsync(
        string toolName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the tool cache from all services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RefreshToolsAsync(CancellationToken cancellationToken = default);
}
