namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Event arguments for state transition events.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class StateTransitionEventArgs<TState> : EventArgs where TState : class
{
    /// <summary>
    /// Gets the source state.
    /// </summary>
    public string FromState { get; }

    /// <summary>
    /// Gets the target state.
    /// </summary>
    public string ToState { get; }

    /// <summary>
    /// Gets the state context.
    /// </summary>
    public TState Context { get; }

    /// <summary>
    /// Gets the timestamp of the transition.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateTransitionEventArgs{TState}"/> class.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="context">The state context.</param>
    public StateTransitionEventArgs(string fromState, string toState, TState context)
    {
        FromState = fromState ?? throw new ArgumentNullException(nameof(fromState));
        ToState = toState ?? throw new ArgumentNullException(nameof(toState));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Timestamp = DateTimeOffset.UtcNow;
    }
}
