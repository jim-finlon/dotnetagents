// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.Messaging;
using DotNetAgents.Agents.Registry;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Integration between state machines and the message bus for state transition triggers.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class MessageBusStateMachineIntegration<TState> where TState : class
{
    private readonly IAgentMessageBus _messageBus;
    private readonly AgentStateMachineRegistry<TState> _stateMachineRegistry;
    private readonly ILogger<MessageBusStateMachineIntegration<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBusStateMachineIntegration{TState}"/> class.
    /// </summary>
    /// <param name="messageBus">The message bus.</param>
    /// <param name="stateMachineRegistry">The state machine registry.</param>
    /// <param name="logger">Optional logger instance.</param>
    public MessageBusStateMachineIntegration(
        IAgentMessageBus messageBus,
        AgentStateMachineRegistry<TState> stateMachineRegistry,
        ILogger<MessageBusStateMachineIntegration<TState>>? logger = null)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _stateMachineRegistry = stateMachineRegistry ?? throw new ArgumentNullException(nameof(stateMachineRegistry));
        _logger = logger;
    }

    /// <summary>
    /// Subscribes to state transition messages and triggers state machine transitions.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    public async Task<IDisposable> SubscribeToStateTransitionsAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        var stateMachine = _stateMachineRegistry.GetStateMachine(agentId);
        if (stateMachine == null)
        {
            throw new InvalidOperationException($"No state machine registered for agent '{agentId}'.");
        }

        return await _messageBus.SubscribeAsync(
            agentId,
            async (agentMessage, ct) =>
            {
                if (agentMessage.MessageType == "StateTransition")
                {
                    try
                    {
                        if (agentMessage.Payload is StateTransitionMessage transitionMessage &&
                            transitionMessage.TargetAgentId == agentId)
                        {
                            // Get the state context - in a real implementation, you'd retrieve this from storage
                            // For now, we'll use a placeholder object
                            var context = new object() as TState ?? throw new InvalidOperationException("Cannot create state context");

                            await stateMachine.TransitionAsync(transitionMessage.TargetState, context, ct).ConfigureAwait(false);
                            _logger?.LogInformation("State transition triggered via message bus for agent '{AgentId}' to state '{State}'",
                                agentId, transitionMessage.TargetState);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to process state transition message for agent '{AgentId}'", agentId);
                    }
                }
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a state transition request to an agent via the message bus.
    /// </summary>
    /// <param name="targetAgentId">The target agent identifier.</param>
    /// <param name="targetState">The target state to transition to.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendStateTransitionRequestAsync(
        string targetAgentId,
        string targetState,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetAgentId);
        ArgumentException.ThrowIfNullOrEmpty(targetState);

        var message = new StateTransitionMessage
        {
            TargetAgentId = targetAgentId,
            TargetState = targetState,
            Timestamp = DateTimeOffset.UtcNow
        };

        var agentMessage = new AgentMessage
        {
            FromAgentId = "StateMachineIntegration",
            ToAgentId = targetAgentId,
            MessageType = "StateTransition",
            Payload = message,
            Timestamp = DateTimeOffset.UtcNow
        };
        await _messageBus.SendAsync(agentMessage, cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("Sent state transition request to agent '{AgentId}' for state '{State}'", targetAgentId, targetState);
    }
}

/// <summary>
/// Message for requesting state transitions via message bus.
/// </summary>
public class StateTransitionMessage
{
    /// <summary>
    /// Gets or sets the target agent identifier.
    /// </summary>
    public string TargetAgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target state to transition to.
    /// </summary>
    public string TargetState { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
