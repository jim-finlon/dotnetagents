// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Voice.Dialog.StateMachines;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Dialog;

/// <summary>
/// Default implementation of <see cref="IDialogManager"/>.
/// </summary>
public class DialogManager : IDialogManager
{
    private readonly IDialogStore _store;
    private readonly Dictionary<string, IDialogHandler> _handlers;
    private readonly ILogger<DialogManager> _logger;
    private readonly IDialogStateMachine<DialogContext>? _stateMachine;
    private readonly Dictionary<Guid, DialogContext> _dialogContexts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogManager"/> class.
    /// </summary>
    /// <param name="store">The dialog store.</param>
    /// <param name="handlers">The dictionary of dialog handlers.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="stateMachine">Optional dialog state machine for tracking dialog lifecycle.</param>
    public DialogManager(
        IDialogStore store,
        IEnumerable<IDialogHandler> handlers,
        ILogger<DialogManager> logger,
        IDialogStateMachine<DialogContext>? stateMachine = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateMachine = stateMachine;

        _handlers = handlers.ToDictionary(h => h.DialogType, h => h);
    }

    /// <inheritdoc />
    public async Task<DialogState> StartDialogAsync(
        Guid userId,
        string dialogType,
        Dictionary<string, object>? initialContext = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dialogType))
        {
            throw new ArgumentException("Dialog type cannot be null or empty", nameof(dialogType));
        }

        if (!_handlers.TryGetValue(dialogType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for dialog type: {dialogType}");
        }

        _logger.LogInformation("Starting dialog {DialogType} for user {UserId}", dialogType, userId);

        // Initialize state machine context if available
        DialogContext? dialogContext = null;
        if (_stateMachine != null)
        {
            dialogContext = new DialogContext
            {
                DialogId = Guid.NewGuid(),
                UserId = userId,
                DialogType = dialogType
            };

            lock (_dialogContexts)
            {
                _dialogContexts[dialogContext.DialogId] = dialogContext;
            }

            try
            {
                await _stateMachine.TransitionAsync("Initial", dialogContext, cancellationToken)
                    .ConfigureAwait(false);
                await _stateMachine.TransitionAsync("CollectingInfo", dialogContext, cancellationToken)
                    .ConfigureAwait(false);
                dialogContext.CollectingInfoStartedAt = DateTimeOffset.UtcNow;
                _logger.LogDebug("Dialog {DialogId} transitioned to CollectingInfo", dialogContext.DialogId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to transition dialog state machine");
            }
        }

        var initialState = await handler.InitializeAsync(userId, initialContext, cancellationToken)
            .ConfigureAwait(false);

        // Update dialog context with actual dialog ID
        if (dialogContext != null)
        {
            dialogContext.DialogId = initialState.DialogId;
            lock (_dialogContexts)
            {
                _dialogContexts[initialState.DialogId] = dialogContext;
            }
        }

        await _store.CreateAsync(initialState, cancellationToken).ConfigureAwait(false);

        return initialState;
    }

    /// <inheritdoc />
    public async Task<DialogState> ContinueDialogAsync(
        Guid dialogId,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new ArgumentException("User input cannot be null or empty", nameof(userInput));
        }

        var state = await _store.GetAsync(dialogId, cancellationToken).ConfigureAwait(false);
        if (state == null)
        {
            throw new InvalidOperationException($"Dialog {dialogId} not found");
        }

        if (state.Status == DialogStatus.Completed || state.Status == DialogStatus.Cancelled)
        {
            throw new InvalidOperationException($"Dialog {dialogId} is already {state.Status}");
        }

        if (!_handlers.TryGetValue(state.DialogType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for dialog type: {state.DialogType}");
        }

        _logger.LogDebug("Processing input for dialog {DialogId}", dialogId);

        // Get or create dialog context for state machine tracking
        DialogContext? dialogContext = null;
        if (_stateMachine != null)
        {
            lock (_dialogContexts)
            {
                if (!_dialogContexts.TryGetValue(dialogId, out dialogContext))
                {
                    dialogContext = new DialogContext
                    {
                        DialogId = dialogId,
                        UserId = state.UserId,
                        DialogType = state.DialogType
                    };
                    _dialogContexts[dialogId] = dialogContext;
                }
            }
        }

        var updatedState = await handler.ProcessInputAsync(state, userInput, cancellationToken)
            .ConfigureAwait(false);

        // Update dialog context
        if (dialogContext != null)
        {
            dialogContext.QuestionsAsked = state.PendingQuestions.Count - updatedState.PendingQuestions.Count;
            dialogContext.QuestionsRemaining = updatedState.PendingQuestions.Count;
        }

        // Check if dialog is complete
        var isComplete = await handler.IsCompleteAsync(updatedState, cancellationToken).ConfigureAwait(false);
        if (isComplete)
        {
            updatedState = updatedState with
            {
                Status = DialogStatus.Completed,
                CompletedAt = DateTime.UtcNow
            };

            // Transition to Completed state
            if (_stateMachine != null && dialogContext != null)
            {
                try
                {
                    dialogContext.AllInfoCollected = true;
                    if (dialogContext.RequiresConfirmation && !dialogContext.Confirmed)
                    {
                        // Transition to Confirming if confirmation is needed
                        dialogContext.ConfirmingStartedAt = DateTimeOffset.UtcNow;
                        await _stateMachine.TransitionAsync("Confirming", dialogContext, cancellationToken)
                            .ConfigureAwait(false);
                        _logger.LogDebug("Dialog {DialogId} transitioned to Confirming", dialogId);
                    }
                    else
                    {
                        // Transition directly to Executing
                        dialogContext.ExecutingStartedAt = DateTimeOffset.UtcNow;
                        await _stateMachine.TransitionAsync("Executing", dialogContext, cancellationToken)
                            .ConfigureAwait(false);
                        _logger.LogDebug("Dialog {DialogId} transitioned to Executing", dialogId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to transition dialog state machine");
                }
            }
        }
        else
        {
            // Get next question
            var nextQuestion = await handler.GetNextQuestionAsync(updatedState, cancellationToken)
                .ConfigureAwait(false);
            updatedState = updatedState with
            {
                Status = DialogStatus.WaitingForInput,
                CurrentQuestion = nextQuestion
            };

            // Stay in CollectingInfo state
            if (_stateMachine != null && dialogContext != null)
            {
                try
                {
                    // Ensure we're in CollectingInfo state
                    if (dialogContext.QuestionsRemaining > 0)
                    {
                        await _stateMachine.TransitionAsync("CollectingInfo", dialogContext, cancellationToken)
                            .ConfigureAwait(false);
                        _logger.LogDebug("Dialog {DialogId} remains in CollectingInfo", dialogId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to transition dialog state machine");
                }
            }
        }

        await _store.UpdateAsync(updatedState, cancellationToken).ConfigureAwait(false);

        // Transition to Completed if dialog is done
        if (isComplete && _stateMachine != null && dialogContext != null)
        {
            try
            {
                await _stateMachine.TransitionAsync("Completed", dialogContext, cancellationToken)
                    .ConfigureAwait(false);
                _logger.LogDebug("Dialog {DialogId} transitioned to Completed", dialogId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to transition dialog to Completed state");
            }
        }

        return updatedState;
    }

    /// <inheritdoc />
    public Task<DialogState?> GetDialogStateAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(dialogId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DialogState> CompleteDialogAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default)
    {
        var state = await _store.GetAsync(dialogId, cancellationToken).ConfigureAwait(false);
        if (state == null)
        {
            throw new InvalidOperationException($"Dialog {dialogId} not found");
        }

        // Transition to Completed state
        if (_stateMachine != null)
        {
            lock (_dialogContexts)
            {
                if (_dialogContexts.TryGetValue(dialogId, out var dialogContext))
                {
                    try
                    {
                        _ = Task.Run(async () =>
                        {
                            await _stateMachine.TransitionAsync("Completed", dialogContext, cancellationToken)
                                .ConfigureAwait(false);
                            _logger.LogDebug("Dialog {DialogId} transitioned to Completed", dialogId);
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to transition dialog to Completed state");
                    }
                }
            }
        }

        var completedState = state with
        {
            Status = DialogStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _store.UpdateAsync(completedState, cancellationToken).ConfigureAwait(false);

        return completedState;
    }

    /// <inheritdoc />
    public async Task CancelDialogAsync(
        Guid dialogId,
        CancellationToken cancellationToken = default)
    {
        var state = await _store.GetAsync(dialogId, cancellationToken).ConfigureAwait(false);
        if (state == null)
        {
            throw new InvalidOperationException($"Dialog {dialogId} not found");
        }

        var cancelledState = state with
        {
            Status = DialogStatus.Cancelled,
            LastUpdatedAt = DateTime.UtcNow
        };

        await _store.UpdateAsync(cancelledState, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<List<DialogState>> GetActiveDialogsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _store.GetActiveDialogsAsync(userId, cancellationToken);
    }
}
