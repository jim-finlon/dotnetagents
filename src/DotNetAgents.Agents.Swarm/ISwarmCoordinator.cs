using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;

namespace DotNetAgents.Agents.Swarm;

/// <summary>
/// Coordinates a swarm of agents using swarm intelligence algorithms.
/// </summary>
public interface ISwarmCoordinator
{
    /// <summary>
    /// Gets the swarm identifier.
    /// </summary>
    string SwarmId { get; }

    /// <summary>
    /// Gets the number of agents in the swarm.
    /// </summary>
    int AgentCount { get; }

    /// <summary>
    /// Adds an agent to the swarm.
    /// </summary>
    /// <param name="agentId">The agent ID to add.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an agent from the swarm.
    /// </summary>
    /// <param name="agentId">The agent ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Distributes a task across the swarm using swarm intelligence.
    /// </summary>
    /// <param name="task">The task to distribute.</param>
    /// <param name="strategy">The swarm coordination strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The task distribution result.</returns>
    Task<SwarmTaskDistribution> DistributeTaskAsync(
        WorkerTask task,
        SwarmCoordinationStrategy strategy = SwarmCoordinationStrategy.ParticleSwarm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets swarm statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Swarm statistics.</returns>
    Task<SwarmStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Swarm coordination strategies.
/// </summary>
public enum SwarmCoordinationStrategy
{
    /// <summary>
    /// Particle Swarm Optimization - agents move toward best solutions.
    /// </summary>
    ParticleSwarm,

    /// <summary>
    /// Ant Colony Optimization - agents follow pheromone trails.
    /// </summary>
    AntColony,

    /// <summary>
    /// Flocking - agents follow neighbors with alignment, cohesion, separation.
    /// </summary>
    Flocking,

    /// <summary>
    /// Consensus-based - agents reach consensus on task assignment.
    /// </summary>
    Consensus
}

/// <summary>
/// Result of task distribution in a swarm.
/// </summary>
public class SwarmTaskDistribution
{
    /// <summary>
    /// Gets or sets the task ID.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of agents assigned to the task.
    /// </summary>
    public List<string> AssignedAgents { get; set; } = new();

    /// <summary>
    /// Gets or sets the distribution strategy used.
    /// </summary>
    public SwarmCoordinationStrategy Strategy { get; set; }

    /// <summary>
    /// Gets or sets the confidence score of the distribution.
    /// </summary>
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// Statistics about a swarm.
/// </summary>
public class SwarmStatistics
{
    /// <summary>
    /// Gets or sets the swarm ID.
    /// </summary>
    public string SwarmId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of agents in the swarm.
    /// </summary>
    public int AgentCount { get; set; }

    /// <summary>
    /// Gets or sets the number of active tasks.
    /// </summary>
    public int ActiveTasks { get; set; }

    /// <summary>
    /// Gets or sets the average task completion time.
    /// </summary>
    public TimeSpan AverageCompletionTime { get; set; }

    /// <summary>
    /// Gets or sets the swarm efficiency score.
    /// </summary>
    public double EfficiencyScore { get; set; }
}
