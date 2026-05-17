namespace DotNetAgents.Mcp.Configuration;

/// <summary>
/// Configuration for an MCP service connection.
/// </summary>
public record McpServiceConfig
{
    /// <summary>
    /// Gets the name of the service.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the base URL of the MCP service.
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Gets the authentication type (none, api_key, jwt).
    /// </summary>
    public string AuthType { get; init; } = "none";

    /// <summary>
    /// Gets the authentication token (if required).
    /// </summary>
    public string? AuthToken { get; init; }

    /// <summary>
    /// Gets the timeout in seconds for requests.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Gets the number of retries for failed requests.
    /// </summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>
    /// Gets the circuit breaker threshold (number of failures before opening).
    /// </summary>
    public int CircuitBreakerThreshold { get; init; } = 5;

    /// <summary>
    /// Gets custom headers to include in requests.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();
}
