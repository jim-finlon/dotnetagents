namespace DotNetAgents.Agents.WorkerPool.LoadBalancing;

/// <summary>
/// Strategy for load balancing tasks across worker agents.
/// </summary>
public enum LoadBalancingStrategy
{
    /// <summary>
    /// Round-robin: Distribute tasks evenly across all available workers in order.
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Capability-based: Select workers based on their capabilities matching task requirements.
    /// </summary>
    CapabilityBased,

    /// <summary>
    /// Priority-based: Select workers based on their current load and priority.
    /// </summary>
    PriorityBased,

    /// <summary>
    /// Random: Randomly select from available workers.
    /// </summary>
    Random
}
