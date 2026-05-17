namespace DotNetAgents.Voice.Scheduling;

/// <summary>
/// Interface for storing and retrieving scheduled commands.
/// </summary>
public interface IScheduledCommandStore
{
    /// <summary>
    /// Creates a new scheduled command.
    /// </summary>
    /// <param name="command">The scheduled command to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created scheduled command.</returns>
    Task<ScheduledCommand> CreateAsync(
        ScheduledCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a scheduled command by ID.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The scheduled command, or null if not found.</returns>
    Task<ScheduledCommand?> GetAsync(
        Guid commandId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a scheduled command.
    /// </summary>
    /// <param name="command">The updated scheduled command.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated scheduled command.</returns>
    Task<ScheduledCommand> UpdateAsync(
        ScheduledCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all commands that are due for execution up to the specified time.
    /// </summary>
    /// <param name="upTo">The maximum execution time to consider.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The list of due commands.</returns>
    Task<List<ScheduledCommand>> GetDueCommandsAsync(
        DateTime upTo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scheduled commands for a user within a date range.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="from">Optional start date.</param>
    /// <param name="to">Optional end date.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The list of scheduled commands.</returns>
    Task<List<ScheduledCommand>> GetByUserAndDateRangeAsync(
        Guid userId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scheduled command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task DeleteAsync(
        Guid commandId,
        CancellationToken cancellationToken = default);
}
