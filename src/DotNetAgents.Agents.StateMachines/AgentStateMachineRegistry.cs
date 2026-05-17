using DotNetAgents.Agents.Registry;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Registry for managing multiple state machines associated with agents.
/// </summary>
/// <typeparam name="TState">The type of the state context.</typeparam>
public class AgentStateMachineRegistry<TState> where TState : class
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly Dictionary<string, IStateMachine<TState>> _stateMachines = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AgentStateMachineRegistry<TState>>? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentStateMachineRegistry{TState}"/> class.
    /// </summary>
    /// <param name="agentRegistry">The agent registry.</param>
    /// <param name="logger">Optional logger instance.</param>
    public AgentStateMachineRegistry(IAgentRegistry agentRegistry, ILogger<AgentStateMachineRegistry<TState>>? logger = null)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _logger = logger;
    }

    private readonly Dictionary<string, string> _agentStates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a state machine for an agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="stateMachine">The state machine instance.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RegisterAsync(string agentId, IStateMachine<TState> stateMachine, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        ArgumentNullException.ThrowIfNull(stateMachine);

        // Verify agent exists
        var agentInfo = await _agentRegistry.GetByIdAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (agentInfo == null)
        {
            throw new InvalidOperationException($"Agent '{agentId}' is not registered.");
        }

        lock (_lock)
        {
            _stateMachines[agentId] = stateMachine;
            _agentStates[agentId] = stateMachine.CurrentState;
        }

        // Subscribe to state machine transition events
        stateMachine.StateTransitioned += (sender, e) =>
        {
            lock (_lock)
            {
                _agentStates[agentId] = e.ToState;
            }
            _logger?.LogDebug("Agent '{AgentId}' state machine transitioned to '{State}'", agentId, e.ToState);
        };

        _logger?.LogInformation("Registered state machine for agent '{AgentId}' with initial state '{State}'", agentId, stateMachine.CurrentState);
    }

    /// <summary>
    /// Gets the state machine for an agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <returns>The state machine instance, or null if not found.</returns>
    public IStateMachine<TState>? GetStateMachine(string agentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        lock (_lock)
        {
            return _stateMachines.TryGetValue(agentId, out var stateMachine) ? stateMachine : null;
        }
    }

    /// <summary>
    /// Unregisters a state machine for an agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    public void Unregister(string agentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        lock (_lock)
        {
            _stateMachines.Remove(agentId);
        }

        _logger?.LogInformation("Unregistered state machine for agent '{AgentId}'", agentId);
    }

    /// <summary>
    /// Gets all registered state machines.
    /// </summary>
    /// <returns>A collection of agent ID and state machine pairs.</returns>
    public IReadOnlyDictionary<string, IStateMachine<TState>> GetAllStateMachines()
    {
        lock (_lock)
        {
            return new Dictionary<string, IStateMachine<TState>>(_stateMachines);
        }
    }

    /// <summary>
    /// Gets the current state machine state for an agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <returns>The current state, or null if the agent doesn't have a registered state machine.</returns>
    public string? GetAgentState(string agentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        lock (_lock)
        {
            return _agentStates.TryGetValue(agentId, out var state) ? state : null;
        }
    }

    /// <summary>
    /// Gets agents that are in a specific state machine state.
    /// </summary>
    /// <param name="state">The state to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of agent info for agents in the specified state.</returns>
    public async Task<IEnumerable<AgentInfo>> GetAgentsByStateAsync(string state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(state);

        var agentIds = new List<string>();
        lock (_lock)
        {
            foreach (var kvp in _agentStates)
            {
                if (kvp.Value.Equals(state, StringComparison.OrdinalIgnoreCase))
                {
                    agentIds.Add(kvp.Key);
                }
            }
        }

        var agents = new List<AgentInfo>();
        foreach (var agentId in agentIds)
        {
            var agentInfo = await _agentRegistry.GetByIdAsync(agentId, cancellationToken).ConfigureAwait(false);
            if (agentInfo != null)
            {
                agents.Add(agentInfo);
            }
        }

        return agents;
    }
}
