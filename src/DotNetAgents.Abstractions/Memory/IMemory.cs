namespace DotNetAgents.Abstractions.Memory;

/// <summary>
/// Interface for memory stores that manage conversation history.
/// </summary>
public interface IMemory
{
    /// <summary>
    /// Adds a message to memory.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task AddMessageAsync(
        MemoryMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent messages from memory.
    /// </summary>
    /// <param name="count">The number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of messages, ordered from oldest to newest.</returns>
    Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all messages from memory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for persistent memory stores that can save and load state.
/// </summary>
public interface IMemoryStore : IMemory
{
    /// <summary>
    /// Saves the current memory state to persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads memory state from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task LoadAsync(CancellationToken cancellationToken = default);
}
