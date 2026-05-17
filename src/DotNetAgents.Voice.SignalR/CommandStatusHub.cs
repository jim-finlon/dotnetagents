using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.SignalR;

/// <summary>
/// SignalR hub for real-time command status updates.
/// </summary>
public class CommandStatusHub : Hub
{
    private readonly ILogger<CommandStatusHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandStatusHub"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public CommandStatusHub(ILogger<CommandStatusHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            // Add user to a group for targeted notifications
            Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            _logger.LogInformation("User {UserId} connected to CommandStatusHub", userId.Value);
        }

        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _logger.LogInformation("User {UserId} disconnected from CommandStatusHub", userId.Value);
        }

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes to updates for a specific command.
    /// </summary>
    /// <param name="commandId">The command ID to subscribe to.</param>
    public async Task SubscribeToCommand(Guid commandId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"command_{commandId}");
        _logger.LogDebug("Client {ConnectionId} subscribed to command {CommandId}", Context.ConnectionId, commandId);
    }

    /// <summary>
    /// Unsubscribes from updates for a specific command.
    /// </summary>
    /// <param name="commandId">The command ID to unsubscribe from.</param>
    public async Task UnsubscribeFromCommand(Guid commandId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"command_{commandId}");
        _logger.LogDebug("Client {ConnectionId} unsubscribed from command {CommandId}", Context.ConnectionId, commandId);
    }

    private Guid? GetUserId()
    {
        // Try to get user ID from claims
        var userIdClaim = Context.User?.FindFirst("sub")?.Value ??
                         Context.User?.FindFirst("userId")?.Value;

        // Try to get from connection metadata/items if available
        if (string.IsNullOrEmpty(userIdClaim) && Context.Items.TryGetValue("userId", out var userIdObj))
        {
            userIdClaim = userIdObj?.ToString();
        }

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}
