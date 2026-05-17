using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.WorkerPool;

/// <summary>
/// Adapter that maps AgentStatus enum values to state machine states and vice versa.
/// This allows WorkerPool to optionally use state machines while maintaining backward compatibility with AgentStatus.
/// </summary>
public static class AgentStatusStateMachineAdapter
{
    /// <summary>
    /// Converts an AgentStatus enum value to a state machine state name.
    /// </summary>
    /// <param name="status">The agent status to convert.</param>
    /// <returns>The corresponding state machine state name.</returns>
    public static string ToStateMachineState(AgentStatus status)
    {
        return status switch
        {
            AgentStatus.Available => "Available",
            AgentStatus.Busy => "Busy",
            AgentStatus.Unavailable => "Unavailable",
            AgentStatus.Error => "Error",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Converts a state machine state name to an AgentStatus enum value.
    /// </summary>
    /// <param name="stateName">The state machine state name to convert.</param>
    /// <returns>The corresponding AgentStatus enum value, or AgentStatus.Unavailable if state is unknown.</returns>
    public static AgentStatus FromStateMachineState(string stateName)
    {
        return stateName switch
        {
            "Available" => AgentStatus.Available,
            "Busy" => AgentStatus.Busy,
            "Unavailable" => AgentStatus.Unavailable,
            "Error" => AgentStatus.Error,
            _ => AgentStatus.Unavailable
        };
    }

    /// <summary>
    /// Checks if an agent's state machine state matches the given AgentStatus.
    /// </summary>
    /// <param name="currentState">The current state machine state name.</param>
    /// <param name="expectedStatus">The expected AgentStatus.</param>
    /// <returns>True if the state machine's current state matches the expected status.</returns>
    public static bool MatchesStatus(string? currentState, AgentStatus expectedStatus)
    {
        if (string.IsNullOrEmpty(currentState))
        {
            return false;
        }

        var expectedState = ToStateMachineState(expectedStatus);
        return string.Equals(currentState, expectedState, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the AgentStatus from a state machine's current state.
    /// </summary>
    /// <param name="currentState">The state machine state name.</param>
    /// <returns>The AgentStatus corresponding to the current state, or AgentStatus.Unavailable if state is unknown.</returns>
    public static AgentStatus GetStatusFromStateMachine(string? currentState)
    {
        if (string.IsNullOrEmpty(currentState))
        {
            return AgentStatus.Unavailable;
        }

        return FromStateMachineState(currentState);
    }

    /// <summary>
    /// Checks if an agent is available based on its state machine state and task count.
    /// </summary>
    /// <param name="currentState">The current state machine state name, or null if no state machine.</param>
    /// <param name="currentTaskCount">The current number of tasks being processed.</param>
    /// <param name="maxConcurrentTasks">The maximum number of concurrent tasks allowed.</param>
    /// <returns>True if the agent is available (state is "Available" and task count is below max).</returns>
    public static bool IsAvailable(
        string? currentState,
        int currentTaskCount,
        int maxConcurrentTasks)
    {
        if (string.IsNullOrEmpty(currentState))
        {
            // Fallback to task count check if no state machine
            return currentTaskCount < maxConcurrentTasks;
        }

        var status = GetStatusFromStateMachine(currentState);
        return status == AgentStatus.Available && currentTaskCount < maxConcurrentTasks;
    }
}
