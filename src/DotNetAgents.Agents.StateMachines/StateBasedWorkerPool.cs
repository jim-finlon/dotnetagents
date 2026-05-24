// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.WorkerPool;
using DotNetAgents.Agents.WorkerPool.AutoScaling;
using DotNetAgents.Agents.WorkerPool.LoadBalancing;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Extension of worker pool that uses state machines for agent selection.
/// </summary>
public class StateBasedWorkerPool : IWorkerPool
{
    private readonly IWorkerPool _baseWorkerPool;
    private readonly AgentStateMachineRegistry<object> _stateMachineRegistry;
    private readonly ILogger<StateBasedWorkerPool>? _logger;

    /// <inheritdoc/>
    public int WorkerCount => _baseWorkerPool.WorkerCount;

    /// <inheritdoc/>
    public int AvailableWorkerCount => _baseWorkerPool.AvailableWorkerCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateBasedWorkerPool"/> class.
    /// </summary>
    /// <param name="baseWorkerPool">The base worker pool.</param>
    /// <param name="stateMachineRegistry">The state machine registry.</param>
    /// <param name="logger">Optional logger instance.</param>
    public StateBasedWorkerPool(
        IWorkerPool baseWorkerPool,
        AgentStateMachineRegistry<object> stateMachineRegistry,
        ILogger<StateBasedWorkerPool>? logger = null)
    {
        _baseWorkerPool = baseWorkerPool ?? throw new ArgumentNullException(nameof(baseWorkerPool));
        _stateMachineRegistry = stateMachineRegistry ?? throw new ArgumentNullException(nameof(stateMachineRegistry));
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task AddWorkerAsync(string agentId, CancellationToken cancellationToken = default)
    {
        return _baseWorkerPool.AddWorkerAsync(agentId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task RemoveWorkerAsync(string agentId, CancellationToken cancellationToken = default)
    {
        return _baseWorkerPool.RemoveWorkerAsync(agentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AgentInfo?> GetAvailableWorkerAsync(
        string? requiredCapability = null,
        CancellationToken cancellationToken = default)
    {
        // Get available workers from base pool
        var worker = await _baseWorkerPool.GetAvailableWorkerAsync(requiredCapability, cancellationToken).ConfigureAwait(false);

        if (worker == null)
        {
            return null;
        }

        // Check if worker has a state machine and if it's in an available state
        var stateMachine = _stateMachineRegistry.GetStateMachine(worker.AgentId);
        if (stateMachine != null)
        {
            var currentState = _stateMachineRegistry.GetAgentState(worker.AgentId);
            // Only return workers in "Available" or "Idle" states
            if (currentState != null &&
                !currentState.Equals("Available", StringComparison.OrdinalIgnoreCase) &&
                !currentState.Equals("Idle", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogDebug("Worker '{AgentId}' is in state '{State}', skipping", worker.AgentId, currentState);
                return null;
            }
        }

        return worker;
    }

    /// <summary>
    /// Gets an available worker that is in a specific state machine state.
    /// </summary>
    /// <param name="requiredState">The required state machine state.</param>
    /// <param name="requiredCapability">Optional required capability.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An available worker in the specified state, or null if none found.</returns>
    public async Task<AgentInfo?> GetAvailableWorkerInStateAsync(
        string requiredState,
        string? requiredCapability = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(requiredState);

        var agentsInState = await _stateMachineRegistry.GetAgentsByStateAsync(requiredState, cancellationToken).ConfigureAwait(false);

        foreach (var agent in agentsInState)
        {
            // Check capability if specified
            if (!string.IsNullOrEmpty(requiredCapability))
            {
                if (!agent.Capabilities.SupportedTools.Contains(requiredCapability) &&
                    !agent.Capabilities.SupportedIntents.Contains(requiredCapability))
                {
                    continue;
                }
            }

            // Check if agent is available (not busy)
            if (agent.Status == AgentStatus.Available &&
                agent.CurrentTaskCount < agent.Capabilities.MaxConcurrentTasks)
            {
                return agent;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public Task<WorkerPoolStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _baseWorkerPool.GetStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ScalingDecision> EvaluateAutoScalingAsync(CancellationToken cancellationToken = default)
    {
        return _baseWorkerPool.EvaluateAutoScalingAsync(cancellationToken);
    }
}
