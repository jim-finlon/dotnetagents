namespace DotNetAgents.Voice.Dialog;

/// <summary>
/// Interface for handling specific types of dialogs.
/// </summary>
public interface IDialogHandler
{
    /// <summary>
    /// Gets the dialog type this handler supports.
    /// </summary>
    string DialogType { get; }

    /// <summary>
    /// Initializes a new dialog session.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="initialContext">Optional initial context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The initial dialog state.</returns>
    Task<DialogState> InitializeAsync(
        Guid userId,
        Dictionary<string, object>? initialContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes user input and updates the dialog state.
    /// </summary>
    /// <param name="currentState">The current dialog state.</param>
    /// <param name="userInput">The user's input.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated dialog state.</returns>
    Task<DialogState> ProcessInputAsync(
        DialogState currentState,
        string userInput,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if the dialog is complete.
    /// </summary>
    /// <param name="state">The current dialog state.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the dialog is complete, false otherwise.</returns>
    Task<bool> IsCompleteAsync(
        DialogState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next question to ask the user.
    /// </summary>
    /// <param name="state">The current dialog state.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The next question, or null if no more questions.</returns>
    Task<string?> GetNextQuestionAsync(
        DialogState state,
        CancellationToken cancellationToken = default);
}
