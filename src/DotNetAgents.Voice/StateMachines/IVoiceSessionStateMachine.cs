namespace DotNetAgents.Voice.StateMachines;

/// <summary>
/// Interface for voice session state machine operations.
/// This interface is defined in Voice to avoid circular dependencies.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public interface IVoiceSessionStateMachine<TState> where TState : class
{
    /// <summary>
    /// Gets the current state of the voice session.
    /// </summary>
    string? CurrentState { get; }

    /// <summary>
    /// Transitions the voice session to a new state.
    /// </summary>
    /// <param name="toState">The target state name.</param>
    /// <param name="context">The state context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default);
}
