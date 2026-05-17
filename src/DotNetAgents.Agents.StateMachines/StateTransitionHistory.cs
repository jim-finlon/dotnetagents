namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a state transition in the history.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class StateTransitionHistory<TState> where TState : class
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
    /// Gets the timestamp of the transition.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets optional metadata about the transition.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StateTransitionHistory{TState}"/> class.
    /// </summary>
    public StateTransitionHistory()
    {
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateTransitionHistory{TState}"/> class.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    public StateTransitionHistory(string fromState, string toState)
    {
        FromState = fromState;
        ToState = toState;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
