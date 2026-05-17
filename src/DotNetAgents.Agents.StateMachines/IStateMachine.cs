namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a state machine that manages state transitions for an agent.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public interface IStateMachine<TState> where TState : class
{
    /// <summary>
    /// Gets the current state of the state machine.
    /// </summary>
    string CurrentState { get; }

    /// <summary>
    /// Determines whether a transition from one state to another is allowed.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="context">The state context.</param>
    /// <returns>True if the transition is allowed; otherwise, false.</returns>
    bool CanTransition(string fromState, string toState, TState context);

    /// <summary>
    /// Transitions to the specified state asynchronously.
    /// </summary>
    /// <param name="toState">The target state.</param>
    /// <param name="context">The state context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available transitions from the current state.
    /// </summary>
    /// <param name="context">The state context.</param>
    /// <returns>A collection of available target states.</returns>
    IEnumerable<string> GetAvailableTransitions(TState context);

    /// <summary>
    /// Event raised when a state transition occurs.
    /// </summary>
    event EventHandler<StateTransitionEventArgs<TState>>? StateTransitioned;

    /// <summary>
    /// Resets the state machine to its initial state.
    /// </summary>
    /// <param name="context">The state context.</param>
    void Reset(TState context);
}
