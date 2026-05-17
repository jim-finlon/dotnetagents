namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Provides persistence for state machine state.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public interface IStateMachinePersistence<TState> where TState : class
{
    /// <summary>
    /// Saves the current state of a state machine.
    /// </summary>
    /// <param name="machineId">The unique identifier of the state machine.</param>
    /// <param name="state">The current state name.</param>
    /// <param name="context">The state context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveStateAsync(string machineId, string state, TState context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the saved state of a state machine.
    /// </summary>
    /// <param name="machineId">The unique identifier of the state machine.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The saved state information, or null if not found.</returns>
    Task<SavedState<TState>?> LoadStateAsync(string machineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the saved state of a state machine.
    /// </summary>
    /// <param name="machineId">The unique identifier of the state machine.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteStateAsync(string machineId, CancellationToken cancellationToken = default);
}
