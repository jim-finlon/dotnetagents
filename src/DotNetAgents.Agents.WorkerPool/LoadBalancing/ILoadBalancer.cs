using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;

namespace DotNetAgents.Agents.WorkerPool.LoadBalancing;

/// <summary>
/// Interface for load balancing worker selection.
/// </summary>
public interface ILoadBalancer
{
    /// <summary>
    /// Selects a worker agent for the given task based on the load balancing strategy.
    /// </summary>
    /// <param name="availableWorkers">List of available worker agents.</param>
    /// <param name="task">The task to assign.</param>
    /// <param name="strategy">The load balancing strategy to use.</param>
    /// <returns>The selected worker agent, or null if no suitable worker is available.</returns>
    AgentInfo? SelectWorker(
        IReadOnlyList<AgentInfo> availableWorkers,
        WorkerTask task,
        LoadBalancingStrategy strategy);
}
