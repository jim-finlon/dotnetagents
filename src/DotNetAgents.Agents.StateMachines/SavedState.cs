namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Represents a saved state machine state.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class SavedState<TState> where TState : class
{
    /// <summary>
    /// Gets the machine identifier.
    /// </summary>
    public string MachineId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the state name.
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// Gets the state context.
    /// </summary>
    public TState Context { get; init; } = null!;

    /// <summary>
    /// Gets the timestamp when the state was saved.
    /// </summary>
    public DateTimeOffset SavedAt { get; init; }
}
