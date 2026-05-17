using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.Messaging;

/// <summary>
/// Message bus for agent-to-agent communication.
/// </summary>
public interface IAgentMessageBus
{
    /// <summary>
    /// Sends a message to a specific agent.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the send operation.</returns>
    Task<MessageSendResult> SendAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a message to all agents or agents matching a filter.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="filter">Optional filter function to select which agents receive the message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The result of the broadcast operation.</returns>
    Task<MessageSendResult> BroadcastAsync(
        AgentMessage message,
        Func<AgentInfo, bool>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages for a specific agent.
    /// </summary>
    /// <param name="agentId">The ID of the agent to subscribe for.</param>
    /// <param name="handler">The handler function to process received messages.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    Task<IDisposable> SubscribeAsync(
        string agentId,
        Func<AgentMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to messages by type.
    /// </summary>
    /// <param name="messageType">The type of messages to subscribe to.</param>
    /// <param name="handler">The handler function to process received messages.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    Task<IDisposable> SubscribeByTypeAsync(
        string messageType,
        Func<AgentMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}
