namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a state definition in a state machine.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class StateDefinition<TState> where TState : class
{
    /// <summary>
    /// Gets the name of the state.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the action to execute when entering this state.
    /// </summary>
    public Action<TState>? EntryAction { get; set; }

    /// <summary>
    /// Gets or sets the action to execute when exiting this state.
    /// </summary>
    public Action<TState>? ExitAction { get; set; }

    /// <summary>
    /// Gets or sets the asynchronous action to execute when entering this state.
    /// </summary>
    public Func<TState, CancellationToken, Task>? EntryActionAsync { get; set; }

    /// <summary>
    /// Gets or sets the asynchronous action to execute when exiting this state.
    /// </summary>
    public Func<TState, CancellationToken, Task>? ExitActionAsync { get; set; }

    /// <summary>
    /// Event raised when entering this state.
    /// </summary>
    public event EventHandler<StateEventArgs<TState>>? OnEntry;

    /// <summary>
    /// Event raised when exiting this state.
    /// </summary>
    public event EventHandler<StateEventArgs<TState>>? OnExit;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateDefinition{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the state.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
    public StateDefinition(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "State name cannot be null or empty.");
        }

        Name = name;
    }

    /// <summary>
    /// Raises the OnEntry event.
    /// </summary>
    /// <param name="context">The state context.</param>
    internal void RaiseOnEntry(TState context)
    {
        OnEntry?.Invoke(this, new StateEventArgs<TState>(context));
    }

    /// <summary>
    /// Raises the OnExit event.
    /// </summary>
    /// <param name="context">The state context.</param>
    internal void RaiseOnExit(TState context)
    {
        OnExit?.Invoke(this, new StateEventArgs<TState>(context));
    }
}
