namespace DotNetAgents.Voice.Dialog.StateMachines;

/// <summary>
/// Interface for dialog state machine operations.
/// This interface is defined in Dialog to avoid circular dependencies.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public interface IDialogStateMachine<TState> where TState : class
{
    /// <summary>
    /// Gets the current state of the dialog.
    /// </summary>
    string? CurrentState { get; }

    /// <summary>
    /// Transitions the dialog to a new state.
    /// </summary>
    /// <param name="toState">The target state name.</param>
    /// <param name="context">The state context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TransitionAsync(string toState, TState context, CancellationToken cancellationToken = default);
}
