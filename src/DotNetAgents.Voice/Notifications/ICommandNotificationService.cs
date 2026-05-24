// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Voice.Orchestration;

namespace DotNetAgents.Voice.Notifications;

/// <summary>
/// Sends real-time notifications about command workflow status (SignalR and other hosts implement this contract).
/// </summary>
public interface ICommandNotificationService
{
    /// <summary>Sends a status update for a command.</summary>
    Task SendStatusUpdateAsync(
        Guid userId,
        Guid commandId,
        CommandStatus status,
        string? message = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a clarification request for a command.</summary>
    Task SendClarificationRequestAsync(
        Guid userId,
        Guid commandId,
        string prompt,
        string missingParameter,
        int turn = 1,
        int maxTurns = 10,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a confirmation request for a command.</summary>
    Task SendConfirmationRequestAsync(
        Guid userId,
        Guid commandId,
        string readBackText,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a command completion notification.</summary>
    Task SendCompletionAsync(
        Guid userId,
        Guid commandId,
        object? result,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a command error notification.</summary>
    Task SendErrorAsync(
        Guid userId,
        Guid commandId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
