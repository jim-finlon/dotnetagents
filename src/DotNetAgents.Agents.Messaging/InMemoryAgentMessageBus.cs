// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.Registry;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Messaging;

/// <summary>
/// In-memory implementation of <see cref="IAgentMessageBus"/>.
/// Suitable for single-instance deployments.
/// </summary>
public class InMemoryAgentMessageBus : IAgentMessageBus
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<InMemoryAgentMessageBus>? _logger;
    private readonly Dictionary<string, List<MessageSubscription>> _agentSubscriptions = new();
    private readonly Dictionary<string, List<MessageSubscription>> _typeSubscriptions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAgentMessageBus"/> class.
    /// </summary>
    /// <param name="agentRegistry">The agent registry for agent lookup.</param>
    /// <param name="logger">Optional logger instance.</param>
    public InMemoryAgentMessageBus(
        IAgentRegistry agentRegistry,
        ILogger<InMemoryAgentMessageBus>? logger = null)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<MessageSendResult> SendAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(message.ToAgentId))
        {
            return Task.FromResult(MessageSendResult.FailureResult(
                message.MessageId,
                "ToAgentId cannot be empty for direct send."));
        }

        if (message.ToAgentId == "*")
        {
            return BroadcastAsync(message, null, cancellationToken);
        }

        lock (_lock)
        {
            // Check if message has expired
            if (message.TimeToLive.HasValue &&
                DateTimeOffset.UtcNow - message.Timestamp > message.TimeToLive.Value)
            {
                _logger?.LogWarning(
                    "Message {MessageId} has expired and will not be delivered",
                    message.MessageId);
                return Task.FromResult(MessageSendResult.FailureResult(
                    message.MessageId,
                    "Message has expired."));
            }

            // Deliver to agent-specific subscribers
            if (_agentSubscriptions.TryGetValue(message.ToAgentId, out var subscriptions))
            {
                var tasks = subscriptions.Select(sub => DeliverMessageAsync(sub, message, cancellationToken));
                Task.Run(async () => await Task.WhenAll(tasks).ConfigureAwait(false), cancellationToken);
            }

            // Deliver to type-specific subscribers (same as broadcast path)
            if (_typeSubscriptions.TryGetValue(message.MessageType, out var typeSubs))
            {
                var typeTasks = typeSubs.Select(sub => DeliverMessageAsync(sub, message, cancellationToken));
                Task.Run(async () => await Task.WhenAll(typeTasks).ConfigureAwait(false), cancellationToken);
            }

            _logger?.LogDebug(
                "Sent message {MessageId} from {FromAgentId} to {ToAgentId}",
                message.MessageId,
                message.FromAgentId,
                message.ToAgentId);
        }

        return Task.FromResult(MessageSendResult.SuccessResult(message.MessageId));
    }

    /// <inheritdoc />
    public Task<MessageSendResult> BroadcastAsync(
        AgentMessage message,
        Func<AgentInfo, bool>? filter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            // Check if message has expired
            if (message.TimeToLive.HasValue &&
                DateTimeOffset.UtcNow - message.Timestamp > message.TimeToLive.Value)
            {
                _logger?.LogWarning(
                    "Broadcast message {MessageId} has expired and will not be delivered",
                    message.MessageId);
                return Task.FromResult(MessageSendResult.FailureResult(
                    message.MessageId,
                    "Message has expired."));
            }

            var allAgents = _agentRegistry.GetAllAsync(cancellationToken).GetAwaiter().GetResult();
            var targetAgents = filter != null
                ? allAgents.Where(filter).ToList()
                : allAgents.ToList();

            var deliveryTasks = new List<Task>();

            // Deliver to agent-specific subscribers
            foreach (var agent in targetAgents)
            {
                if (_agentSubscriptions.TryGetValue(agent.AgentId, out var subscriptions))
                {
                    foreach (var subscription in subscriptions)
                    {
                        deliveryTasks.Add(DeliverMessageAsync(subscription, message, cancellationToken));
                    }
                }
            }

            // Deliver to type-specific subscribers
            if (_typeSubscriptions.TryGetValue(message.MessageType, out var typeSubs))
            {
                foreach (var subscription in typeSubs)
                {
                    deliveryTasks.Add(DeliverMessageAsync(subscription, message, cancellationToken));
                }
            }

            Task.Run(async () => await Task.WhenAll(deliveryTasks).ConfigureAwait(false), cancellationToken);

            _logger?.LogDebug(
                "Broadcast message {MessageId} from {FromAgentId} to {Count} agents",
                message.MessageId,
                message.FromAgentId,
                targetAgents.Count);
        }

        return Task.FromResult(MessageSendResult.SuccessResult(message.MessageId));
    }

    /// <inheritdoc />
    public Task<IDisposable> SubscribeAsync(
        string agentId,
        Func<AgentMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        var subscription = new MessageSubscription(agentId, handler);

        lock (_lock)
        {
            if (!_agentSubscriptions.TryGetValue(agentId, out var subscriptions))
            {
                subscriptions = new List<MessageSubscription>();
                _agentSubscriptions[agentId] = subscriptions;
            }

            subscriptions.Add(subscription);

            _logger?.LogDebug("Subscribed handler for agent {AgentId}", agentId);
        }

        return Task.FromResult<IDisposable>(new SubscriptionDisposable(
            () =>
            {
                lock (_lock)
                {
                    if (_agentSubscriptions.TryGetValue(agentId, out var subscriptions))
                    {
                        subscriptions.Remove(subscription);
                        if (subscriptions.Count == 0)
                        {
                            _agentSubscriptions.Remove(agentId);
                        }
                    }
                }
            }));
    }

    /// <inheritdoc />
    public Task<IDisposable> SubscribeByTypeAsync(
        string messageType,
        Func<AgentMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(messageType);
        ArgumentNullException.ThrowIfNull(handler);
        cancellationToken.ThrowIfCancellationRequested();

        var subscription = new MessageSubscription(messageType, handler, isTypeSubscription: true);

        lock (_lock)
        {
            if (!_typeSubscriptions.TryGetValue(messageType, out var subscriptions))
            {
                subscriptions = new List<MessageSubscription>();
                _typeSubscriptions[messageType] = subscriptions;
            }

            subscriptions.Add(subscription);

            _logger?.LogDebug("Subscribed handler for message type {MessageType}", messageType);
        }

        return Task.FromResult<IDisposable>(new SubscriptionDisposable(
            () =>
            {
                lock (_lock)
                {
                    if (_typeSubscriptions.TryGetValue(messageType, out var subscriptions))
                    {
                        subscriptions.Remove(subscription);
                        if (subscriptions.Count == 0)
                        {
                            _typeSubscriptions.Remove(messageType);
                        }
                    }
                }
            }));
    }

    private async Task DeliverMessageAsync(
        MessageSubscription subscription,
        AgentMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await subscription.Handler(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error delivering message {MessageId} to subscription {SubscriptionId}",
                message.MessageId,
                subscription.Id);
        }
    }

    private sealed class MessageSubscription
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Key { get; }
        public Func<AgentMessage, CancellationToken, Task> Handler { get; }
        public bool IsTypeSubscription { get; }

        public MessageSubscription(
            string key,
            Func<AgentMessage, CancellationToken, Task> handler,
            bool isTypeSubscription = false)
        {
            Key = key;
            Handler = handler;
            IsTypeSubscription = isTypeSubscription;
        }
    }

    private sealed class SubscriptionDisposable : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public SubscriptionDisposable(Action unsubscribe)
        {
            _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe();
                _disposed = true;
            }
        }
    }
}
