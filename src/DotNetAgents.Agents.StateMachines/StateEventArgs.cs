namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Event arguments for state machine events.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class StateEventArgs<TState> : EventArgs where TState : class
{
    /// <summary>
    /// Gets the state context.
    /// </summary>
    public TState Context { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateEventArgs{TState}"/> class.
    /// </summary>
    /// <param name="context">The state context.</param>
    public StateEventArgs(TState context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
}
