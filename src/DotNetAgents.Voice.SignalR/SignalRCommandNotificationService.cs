using DotNetAgents.Voice.Notifications;
using DotNetAgents.Voice.Orchestration;
using DotNetAgents.Voice.SignalR.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.SignalR;

/// <summary>
/// SignalR-based implementation of command notification service.
/// </summary>
public class SignalRCommandNotificationService : ICommandNotificationService
{
    private readonly IHubContext<CommandStatusHub> _hubContext;
    private readonly ILogger<SignalRCommandNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRCommandNotificationService"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="logger">The logger instance.</param>
    public SignalRCommandNotificationService(
        IHubContext<CommandStatusHub> hubContext,
        ILogger<SignalRCommandNotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendStatusUpdateAsync(
        Guid userId,
        Guid commandId,
        CommandStatus status,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var update = new CommandStatusUpdate
        {
            CommandId = commandId,
            UserId = userId,
            Status = status,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        // Send to user-specific group
        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("StatusUpdate", update, cancellationToken)
            .ConfigureAwait(false);

        // Also send to command-specific group
        await _hubContext.Clients.Group($"command_{commandId}")
            .SendAsync("StatusUpdate", update, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Sent status update for command {CommandId} to user {UserId}: {Status}",
            commandId,
            userId,
            status);
    }

    /// <inheritdoc />
    public async Task SendClarificationRequestAsync(
        Guid userId,
        Guid commandId,
        string prompt,
        string missingParameter,
        int turn = 1,
        int maxTurns = 10,
        CancellationToken cancellationToken = default)
    {
        var request = new ClarificationRequest
        {
            CommandId = commandId,
            UserId = userId,
            Prompt = prompt,
            MissingParameter = missingParameter,
            Turn = turn,
            MaxTurns = maxTurns,
            RequestedAt = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ClarificationRequest", request, cancellationToken)
            .ConfigureAwait(false);

        await _hubContext.Clients.Group($"command_{commandId}")
            .SendAsync("ClarificationRequest", request, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Sent clarification request for command {CommandId} to user {UserId}: {Parameter}",
            commandId,
            userId,
            missingParameter);
    }

    /// <inheritdoc />
    public async Task SendConfirmationRequestAsync(
        Guid userId,
        Guid commandId,
        string readBackText,
        CancellationToken cancellationToken = default)
    {
        var request = new ConfirmationRequest
        {
            CommandId = commandId,
            UserId = userId,
            ReadBackText = readBackText,
            RequestedAt = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ConfirmationRequest", request, cancellationToken)
            .ConfigureAwait(false);

        await _hubContext.Clients.Group($"command_{commandId}")
            .SendAsync("ConfirmationRequest", request, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Sent confirmation request for command {CommandId} to user {UserId}",
            commandId,
            userId);
    }

    /// <inheritdoc />
    public async Task SendCompletionAsync(
        Guid userId,
        Guid commandId,
        object? result,
        CancellationToken cancellationToken = default)
    {
        var update = new CommandStatusUpdate
        {
            CommandId = commandId,
            UserId = userId,
            Status = CommandStatus.Completed,
            Result = result,
            Message = "Command completed successfully",
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("CommandCompleted", update, cancellationToken)
            .ConfigureAwait(false);

        await _hubContext.Clients.Group($"command_{commandId}")
            .SendAsync("CommandCompleted", update, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Sent completion notification for command {CommandId} to user {UserId}",
            commandId,
            userId);
    }

    /// <inheritdoc />
    public async Task SendErrorAsync(
        Guid userId,
        Guid commandId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var update = new CommandStatusUpdate
        {
            CommandId = commandId,
            UserId = userId,
            Status = CommandStatus.Failed,
            Error = errorMessage,
            Message = $"Command failed: {errorMessage}",
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("CommandError", update, cancellationToken)
            .ConfigureAwait(false);

        await _hubContext.Clients.Group($"command_{commandId}")
            .SendAsync("CommandError", update, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogWarning(
            "Sent error notification for command {CommandId} to user {UserId}: {Error}",
            commandId,
            userId,
            errorMessage);
    }
}
