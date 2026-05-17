namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a state transition definition.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public record StateTransition<TState> where TState : class
{
    /// <summary>
    /// Gets the source state.
    /// </summary>
    public string FromState { get; init; } = string.Empty;

    /// <summary>
    /// Gets the target state.
    /// </summary>
    public string ToState { get; init; } = string.Empty;

    /// <summary>
    /// Gets the guard condition that determines if the transition is allowed.
    /// Returns true if the transition is allowed; otherwise, false.
    /// </summary>
    public Func<TState, bool>? Guard { get; init; }

    /// <summary>
    /// Gets the action to execute when the transition occurs.
    /// </summary>
    public Action<TState>? OnTransition { get; init; }

    /// <summary>
    /// Gets the asynchronous action to execute when the transition occurs.
    /// </summary>
    public Func<TState, CancellationToken, Task>? OnTransitionAsync { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateTransition{TState}"/> class.
    /// </summary>
    public StateTransition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateTransition{TState}"/> class.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="guard">Optional guard condition.</param>
    /// <param name="onTransition">Optional transition action.</param>
    /// <param name="onTransitionAsync">Optional asynchronous transition action.</param>
    public StateTransition(
        string fromState,
        string toState,
        Func<TState, bool>? guard = null,
        Action<TState>? onTransition = null,
        Func<TState, CancellationToken, Task>? onTransitionAsync = null)
    {
        FromState = fromState;
        ToState = toState;
        Guard = guard;
        OnTransition = onTransition;
        OnTransitionAsync = onTransitionAsync;
    }
}
