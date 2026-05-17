namespace DotNetAgents.Agents.Registry;

/// <summary>
/// Registry for managing agent discovery and capabilities.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Registers an agent with its capabilities.
    /// </summary>
    /// <param name="capabilities">The agent's capabilities.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterAsync(
        AgentCapabilities capabilities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an agent.
    /// </summary>
    /// <param name="agentId">The ID of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnregisterAsync(
        string agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an agent's status.
    /// </summary>
    /// <param name="agentId">The ID of the agent.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateStatusAsync(
        string agentId,
        AgentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an agent's current task count.
    /// </summary>
    /// <param name="agentId">The ID of the agent.</param>
    /// <param name="taskCount">The current task count.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateTaskCountAsync(
        string agentId,
        int taskCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds agents by capability (tool or intent).
    /// </summary>
    /// <param name="capability">The capability to search for (tool name or intent).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of agents that support the specified capability.</returns>
    Task<IReadOnlyList<AgentInfo>> FindByCapabilityAsync(
        string capability,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds agents by type.
    /// </summary>
    /// <param name="agentType">The type of agent to find.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of agents of the specified type.</returns>
    Task<IReadOnlyList<AgentInfo>> FindByTypeAsync(
        string agentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    /// <param name="agentId">The ID of the agent.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The agent information, or null if not found.</returns>
    Task<AgentInfo?> GetByIdAsync(
        string agentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of all registered agents.</returns>
    Task<IReadOnlyList<AgentInfo>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a heartbeat from an agent.
    /// </summary>
    /// <param name="agentId">The ID of the agent.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordHeartbeatAsync(
        string agentId,
        CancellationToken cancellationToken = default);
}
