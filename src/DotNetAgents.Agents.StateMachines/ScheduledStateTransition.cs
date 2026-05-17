namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a scheduled state transition that occurs at a specific time.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class ScheduledStateTransition<TState> where TState : class
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
    /// Gets the scheduled time for the transition.
    /// </summary>
    public DateTimeOffset ScheduledTime { get; init; }

    /// <summary>
    /// Gets the action to execute when the scheduled time arrives.
    /// </summary>
    public Action<TState>? OnScheduled { get; init; }

    /// <summary>
    /// Gets the asynchronous action to execute when the scheduled time arrives.
    /// </summary>
    public Func<TState, CancellationToken, Task>? OnScheduledAsync { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledStateTransition{TState}"/> class.
    /// </summary>
    /// <param name="fromState">The source state.</param>
    /// <param name="toState">The target state.</param>
    /// <param name="scheduledTime">The scheduled time for the transition.</param>
    /// <param name="onScheduled">Optional action to execute on scheduled time.</param>
    /// <param name="onScheduledAsync">Optional asynchronous action to execute on scheduled time.</param>
    public ScheduledStateTransition(
        string fromState,
        string toState,
        DateTimeOffset scheduledTime,
        Action<TState>? onScheduled = null,
        Func<TState, CancellationToken, Task>? onScheduledAsync = null)
    {
        FromState = fromState;
        ToState = toState;
        ScheduledTime = scheduledTime;
        OnScheduled = onScheduled;
        OnScheduledAsync = onScheduledAsync;
    }
}
