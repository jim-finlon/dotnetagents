namespace DotNetAgents.Voice.Dialog;

/// <summary>
/// Interface for managing dialog sessions.
/// </summary>
public interface IDialogManager
{
    /// <summary>
    /// Starts a new dialog session.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="dialogType">The type of dialog to start.</param>
    /// <param name="initialContext">Optional initial context for the dialog.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The initial dialog state.</returns>
    Task<DialogState> StartDialogAsync(
        Guid userId,
        string dialogType,
        Dictionary<string, object>? initialContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Continues an existing dialog with user input.
    /// </summary>
    /// <param name="dialogId">The dialog identifier.</param>
    /// <param name="userInput">The user's input.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated dialog state.</returns>
    Task<DialogState> ContinueDialogAsync(
        Guid dialogId,
        string userInput,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a dialog.
    /// </summary>
    /// <param name="dialogId">The dialog identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The dialog state, or null if not found.</returns>
    Task<DialogState?> GetDialogStateAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a dialog session.
    /// </summary>
    /// <param name="dialogId">The dialog identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The completed dialog state.</returns>
    Task<DialogState> CompleteDialogAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a dialog session.
    /// </summary>
    /// <param name="dialogId">The dialog identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    Task CancelDialogAsync(
        Guid dialogId,
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
}
