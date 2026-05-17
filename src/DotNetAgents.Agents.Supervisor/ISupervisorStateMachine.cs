namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Interface for supervisor state machine operations.
/// This interface is defined in Supervisor to avoid circular dependencies.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public interface ISupervisorStateMachine<TState> where TState : class
{
    /// <summary>
    /// Gets the current state of the state machine.
    /// </summary>
    string? CurrentState { get; }

    /// <summary>
    /// Transitions the state machine to a new state.
    /// </summary>
    /// <param name="toState">The target state.</param>
    /// <param name="context">The state context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default);
}
