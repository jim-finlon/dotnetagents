namespace DotNetAgents.Agents.Registry;

/// <summary>
/// Represents an agent instance with its current status and capabilities.
/// </summary>
public record AgentInfo
{
    /// <summary>
    /// Gets the unique identifier of the agent.
    /// </summary>
    public string AgentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of the agent.
    /// </summary>
    public string AgentType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current status of the agent.
    /// </summary>
    public AgentStatus Status { get; init; }

    /// <summary>
    /// Gets the capabilities of the agent.
    /// </summary>
    public AgentCapabilities Capabilities { get; init; } = new();

    /// <summary>
    /// Gets the timestamp of the last heartbeat from the agent.
    /// </summary>
    public DateTimeOffset LastHeartbeat { get; init; }

    /// <summary>
    /// Gets the endpoint URL for distributed agents (optional).
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// Gets the current number of tasks being processed by this agent.
    /// </summary>
    public int CurrentTaskCount { get; init; }
}
