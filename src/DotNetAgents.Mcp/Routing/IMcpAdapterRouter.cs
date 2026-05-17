namespace DotNetAgents.Mcp.Routing;

/// <summary>
/// Interface for routing intents to appropriate MCP service adapters.
/// </summary>
public interface IMcpAdapterRouter
{
    /// <summary>
    /// Executes an intent by routing it to the appropriate MCP service.
    /// </summary>
    /// <param name="intent">The intent to execute.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the execution, or null if no result.</returns>
    Task<object?> ExecuteIntentAsync(
        Intent intent,
        CancellationToken cancellationToken = default);
}
