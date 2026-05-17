using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;

namespace DotNetAgents.Agents.WorkerPool.LoadBalancing;

/// <summary>
/// Default implementation of <see cref="ILoadBalancer"/>.
/// </summary>
public class LoadBalancer : ILoadBalancer
{
    private readonly Dictionary<string, int> _roundRobinCounters = new();
    private readonly Random _random = new();

    /// <inheritdoc />
    public AgentInfo? SelectWorker(
        IReadOnlyList<AgentInfo> availableWorkers,
        WorkerTask task,
        LoadBalancingStrategy strategy)
    {
        if (availableWorkers.Count == 0)
            return null;

        return strategy switch
        {
            LoadBalancingStrategy.RoundRobin => SelectRoundRobin(availableWorkers),
            LoadBalancingStrategy.CapabilityBased => SelectCapabilityBased(availableWorkers, task),
            LoadBalancingStrategy.PriorityBased => SelectPriorityBased(availableWorkers, task),
            LoadBalancingStrategy.Random => SelectRandom(availableWorkers),
            _ => SelectRoundRobin(availableWorkers)
        };
    }

    private AgentInfo SelectRoundRobin(IReadOnlyList<AgentInfo> workers)
    {
        var key = "default";
        if (!_roundRobinCounters.ContainsKey(key))
        {
            _roundRobinCounters[key] = 0;
        }

        var index = _roundRobinCounters[key] % workers.Count;
        _roundRobinCounters[key]++;

        return workers[index];
    }

    private AgentInfo? SelectCapabilityBased(IReadOnlyList<AgentInfo> workers, WorkerTask task)
    {
        // If task has a required capability, filter workers by that capability
        if (!string.IsNullOrEmpty(task.RequiredCapability))
        {
            var capableWorkers = workers
                .Where(w => w.Capabilities.SupportedTools.Contains(task.RequiredCapability) ||
                           w.Capabilities.SupportedIntents.Contains(task.RequiredCapability))
                .ToList();

            if (capableWorkers.Count > 0)
            {
                // Among capable workers, select by priority (least loaded)
                return SelectPriorityBased(capableWorkers, task);
            }
        }

        // Fall back to priority-based if no capability requirement or no capable workers
        return SelectPriorityBased(workers, task);
    }

    private AgentInfo SelectPriorityBased(IReadOnlyList<AgentInfo> workers, WorkerTask task)
    {
        // Select worker with lowest current load (task count / max concurrent tasks ratio)
        var workersWithCapacity = workers
            .Where(w => w.CurrentTaskCount < w.Capabilities.MaxConcurrentTasks)
            .ToList();

        if (workersWithCapacity.Count == 0)
        {
            // All workers are at capacity, select the one with lowest load
            return workers
                .OrderBy(w => (double)w.CurrentTaskCount / w.Capabilities.MaxConcurrentTasks)
                .ThenBy(w => w.CurrentTaskCount)
                .First();
        }

        // Select from workers with available capacity
        return workersWithCapacity
            .OrderBy(w => (double)w.CurrentTaskCount / w.Capabilities.MaxConcurrentTasks)
            .ThenBy(w => w.CurrentTaskCount)
            .First();
    }

    private AgentInfo SelectRandom(IReadOnlyList<AgentInfo> workers)
    {
        var index = _random.Next(workers.Count);
        return workers[index];
    }
}
