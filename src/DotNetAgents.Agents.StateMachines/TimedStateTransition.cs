namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a timed state transition that occurs after a specified duration.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class TimedStateTransition<TState> where TState : class
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
    /// Gets the timeout duration.
    /// </summary>
    public TimeSpan Timeout { get; init; }

    /// <summary>
    /// Gets the action to execute when the timeout occurs.
    /// </summary>
    public Action<TState>? OnTimeout { get; init; }

    /// <summary>
    /// Gets the asynchronous action to execute when the timeout occurs.
    /// </summary>
    public Func<TState, CancellationToken, Task>? OnTimeoutAsync { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimedStateTransition{TState}"/> class.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="onTimeout">Optional action to execute on timeout.</param>
    /// <param name="onTimeoutAsync">Optional asynchronous action to execute on timeout.</param>
    public TimedStateTransition(
        string fromState,
        string toState,
        TimeSpan timeout,
        Action<TState>? onTimeout = null,
        Func<TState, CancellationToken, Task>? onTimeoutAsync = null)
    {
        FromState = fromState;
        ToState = toState;
        Timeout = timeout;
        OnTimeout = onTimeout;
        OnTimeoutAsync = onTimeoutAsync;
    }
}
