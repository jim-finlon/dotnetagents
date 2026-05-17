namespace DotNetAgents.Core.Agents.StateMachines;

/// <summary>
/// Interface for agent execution state machine operations.
/// This interface is defined in Core to avoid circular dependencies.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public interface IAgentExecutionStateMachine<TState> where TState : class
{
    /// <summary>
    /// Gets the current state of the agent execution.
    /// </summary>
    string? CurrentState { get; }

    /// <summary>
    /// Transitions the state machine to a new state.
    /// </summary>
    /// <param name="toState">The target state.</param>
    /// <param name="context">The state context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default);
}
