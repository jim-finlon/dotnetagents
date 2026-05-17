namespace DotNetAgents.Voice.Dialog;

/// <summary>
/// Interface for storing and retrieving dialog states.
/// </summary>
public interface IDialogStore
{
    /// <summary>
    /// Creates a new dialog state.
    /// </summary>
    /// <param name="state">The dialog state to create.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created dialog state.</returns>
    Task<DialogState> CreateAsync(
        DialogState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a dialog state by ID.
    /// </summary>
    /// <param name="dialogId">The dialog identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The dialog state, or null if not found.</returns>
    Task<DialogState?> GetAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing dialog state.
    /// </summary>
    /// <param name="state">The updated dialog state.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated dialog state.</returns>
    Task<DialogState> UpdateAsync(
        DialogState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active dialogs for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The list of active dialog states.</returns>
    Task<List<DialogState>> GetActiveDialogsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dialog state.
    /// </summary>
    /// <param name="dialogId">The dialog identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task DeleteAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default);
}
