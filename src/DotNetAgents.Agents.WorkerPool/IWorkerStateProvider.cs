namespace DotNetAgents.Agents.WorkerPool;

/// <summary>
/// Interface for providing worker state information.
/// This allows WorkerPool to use state machines without creating circular dependencies.
/// </summary>
public interface IWorkerStateProvider
{
    /// <summary>
    /// Gets the current state machine state for a worker agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <returns>The current state machine state name, or null if the agent doesn't have a state machine.</returns>
    string? GetAgentState(string agentId);
}
