namespace DotNetAgents.Mcp.Abstractions;

/// <summary>
/// Interface for MCP client factory.
/// </summary>
public interface IMcpClientFactory
{
    /// <summary>
    /// Creates or retrieves an MCP client for the specified service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The MCP client for the service.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
    IMcpClient GetClient(string serviceName);

    /// <summary>
    /// Gets all registered service names.
    /// </summary>
    /// <returns>The list of registered service names.</returns>
    IEnumerable<string> GetRegisteredServices();
}
