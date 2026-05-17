namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Represents the health status of an MCP service.
/// </summary>
public record McpServiceHealth
{
    /// <summary>
    /// Gets the name of the service.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the status of the service (healthy, degraded, down).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the latency of the service in milliseconds.
    /// </summary>
    public long LatencyMs { get; init; }

    /// <summary>
    /// Gets the timestamp of the last health check.
    /// </summary>
    public DateTime LastCheck { get; init; }

    /// <summary>
    /// Gets the timestamp of the last successful health check.
    /// </summary>
    public DateTime? LastSuccess { get; init; }

    /// <summary>
    /// Gets the error rate (0.0 to 1.0).
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// Gets the number of available tools.
    /// </summary>
    public int AvailableTools { get; init; }
}
