using DotNetAgents.Agents.WorkerPool;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// Adapter that implements IWorkerStateProvider using AgentStateMachineRegistry.
/// This allows WorkerPool to use state machines without creating circular dependencies.
/// </summary>
public class WorkerStateProviderAdapter : IWorkerStateProvider
{
    private readonly AgentStateMachineRegistry<object> _stateMachineRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkerStateProviderAdapter"/> class.
    /// </summary>
    /// <param name="stateMachineRegistry">The state machine registry.</param>
    public WorkerStateProviderAdapter(AgentStateMachineRegistry<object> stateMachineRegistry)
    {
        _stateMachineRegistry = stateMachineRegistry ?? throw new ArgumentNullException(nameof(stateMachineRegistry));
    }

    /// <inheritdoc/>
    public string? GetAgentState(string agentId)
    {
        return _stateMachineRegistry.GetAgentState(agentId);
    }
}
