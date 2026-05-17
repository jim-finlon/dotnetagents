using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;
using DotNetAgents.Agents.WorkerPool;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Swarm;

/// <summary>
/// Implements swarm intelligence algorithms for agent coordination.
/// </summary>
public class SwarmCoordinator : ISwarmCoordinator
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IWorkerPool _workerPool;
    private readonly ILogger<SwarmCoordinator>? _logger;
    private readonly HashSet<string> _swarmAgents = new();
    private readonly object _lock = new();
    private readonly Dictionary<string, SwarmAgentState> _agentStates = new();
    private readonly List<TimeSpan> _taskCompletionTimes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SwarmCoordinator"/> class.
    /// </summary>
    /// <param name="swarmId">The unique identifier for this swarm.</param>
    /// <param name="agentRegistry">The agent registry.</param>
    /// <param name="workerPool">The worker pool.</param>
    /// <param name="logger">Optional logger instance.</param>
    public SwarmCoordinator(
        string swarmId,
        IAgentRegistry agentRegistry,
        IWorkerPool workerPool,
        ILogger<SwarmCoordinator>? logger = null)
    {
        SwarmId = swarmId ?? throw new ArgumentNullException(nameof(swarmId));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _workerPool = workerPool ?? throw new ArgumentNullException(nameof(workerPool));
        _logger = logger;
    }

    /// <inheritdoc />
    public string SwarmId { get; }

    /// <inheritdoc />
    public int AgentCount
    {
        get
        {
            lock (_lock)
            {
                return _swarmAgents.Count;
            }
        }
    }

    /// <inheritdoc />
    public async Task AddAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_swarmAgents.Add(agentId))
            {
                _agentStates[agentId] = new SwarmAgentState
                {
                    AgentId = agentId,
                    JoinedAt = DateTimeOffset.UtcNow,
                    TaskCount = 0,
                    SuccessRate = 1.0
                };
                _logger?.LogInformation("Added agent {AgentId} to swarm {SwarmId}", agentId, SwarmId);
            }
        }

        await _workerPool.AddWorkerAsync(agentId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_swarmAgents.Remove(agentId))
            {
                _agentStates.Remove(agentId);
                _logger?.LogInformation("Removed agent {AgentId} from swarm {SwarmId}", agentId, SwarmId);
            }
        }

        return _workerPool.RemoveWorkerAsync(agentId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SwarmTaskDistribution> DistributeTaskAsync(
        WorkerTask task,
        SwarmCoordinationStrategy strategy = SwarmCoordinationStrategy.ParticleSwarm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();

        _logger?.LogInformation(
            "Distributing task {TaskId} in swarm {SwarmId} using strategy {Strategy}",
            task.TaskId,
            SwarmId,
            strategy);

        var availableAgents = await GetAvailableAgentsAsync(cancellationToken).ConfigureAwait(false);

        if (availableAgents.Count == 0)
        {
            return new SwarmTaskDistribution
            {
                TaskId = task.TaskId,
                Strategy = strategy,
                ConfidenceScore = 0.0
            };
        }

        var assignedAgents = strategy switch
        {
            SwarmCoordinationStrategy.ParticleSwarm => DistributeUsingParticleSwarm(task, availableAgents),
            SwarmCoordinationStrategy.AntColony => DistributeUsingAntColony(task, availableAgents),
            SwarmCoordinationStrategy.Flocking => DistributeUsingFlocking(task, availableAgents),
            SwarmCoordinationStrategy.Consensus => await DistributeUsingConsensusAsync(task, availableAgents, cancellationToken).ConfigureAwait(false),
            _ => DistributeUsingParticleSwarm(task, availableAgents)
        };

        // Update agent states
        lock (_lock)
        {
            foreach (var agentId in assignedAgents)
            {
                if (_agentStates.TryGetValue(agentId, out var state))
                {
                    state.TaskCount++;
                    state.LastTaskAssignedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        return new SwarmTaskDistribution
        {
            TaskId = task.TaskId,
            AssignedAgents = assignedAgents,
            Strategy = strategy,
            ConfidenceScore = CalculateConfidenceScore(assignedAgents, availableAgents)
        };
    }

    /// <inheritdoc />
    public Task<SwarmStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var avgCompletionTime = _taskCompletionTimes.Count > 0
                ? TimeSpan.FromMilliseconds(_taskCompletionTimes.Average(t => t.TotalMilliseconds))
                : TimeSpan.Zero;

            var efficiencyScore = CalculateEfficiencyScore();

            return Task.FromResult(new SwarmStatistics
            {
                SwarmId = SwarmId,
                AgentCount = _swarmAgents.Count,
                ActiveTasks = _agentStates.Values.Sum(s => s.TaskCount),
                AverageCompletionTime = avgCompletionTime,
                EfficiencyScore = efficiencyScore
            });
        }
    }

    private async Task<List<AgentInfo>> GetAvailableAgentsAsync(CancellationToken cancellationToken)
    {
        var allAgents = await _agentRegistry.GetAllAsync(cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            return allAgents
                .Where(a => _swarmAgents.Contains(a.AgentId) && a.Status == AgentStatus.Available)
                .ToList();
        }
    }

    private List<string> DistributeUsingParticleSwarm(WorkerTask task, List<AgentInfo> availableAgents)
    {
        // Particle Swarm Optimization: Select agents based on their "fitness" (success rate, availability)
        // Agents with higher fitness are more likely to be selected

        lock (_lock)
        {
            var scoredAgents = availableAgents
                .Select(agent =>
                {
                    var state = _agentStates.GetValueOrDefault(agent.AgentId);
                    var fitness = CalculateFitness(agent, state);
                    return new { AgentId = agent.AgentId, Fitness = fitness };
                })
                .OrderByDescending(a => a.Fitness)
                .ToList();

            // Select top agents (swarm size based on task complexity)
            var count = Math.Min(3, scoredAgents.Count); // Select up to 3 agents
            return scoredAgents.Take(count).Select(a => a.AgentId).ToList();
        }
    }

    private List<string> DistributeUsingAntColony(WorkerTask task, List<AgentInfo> availableAgents)
    {
        // Ant Colony Optimization: Agents with successful task history leave "pheromone trails"
        // Other agents are more likely to follow successful paths

        lock (_lock)
        {
            var pheromoneLevels = availableAgents
                .Select(agent =>
                {
                    var state = _agentStates.GetValueOrDefault(agent.AgentId);
                    var pheromone = state?.SuccessRate ?? 0.5; // Base pheromone level
                    return new { AgentId = agent.AgentId, Pheromone = pheromone };
                })
                .OrderByDescending(a => a.Pheromone)
                .ToList();

            // Select agents with highest pheromone levels
            var count = Math.Min(2, pheromoneLevels.Count);
            return pheromoneLevels.Take(count).Select(a => a.AgentId).ToList();
        }
    }

    private List<string> DistributeUsingFlocking(WorkerTask task, List<AgentInfo> availableAgents)
    {
        // Flocking behavior: Agents align with neighbors, maintain cohesion, avoid separation
        // Select agents that are "close" to each other (similar capabilities)

        if (availableAgents.Count == 0)
            return new List<string>();

        // Group agents by similar capabilities
        var capabilityGroups = availableAgents
            .GroupBy(a => string.Join(",", a.Capabilities.SupportedTools.OrderBy(t => t)))
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (capabilityGroups != null)
        {
            return capabilityGroups.Take(3).Select(a => a.AgentId).ToList();
        }

        return availableAgents.Take(1).Select(a => a.AgentId).ToList();
    }

    private async Task<List<string>> DistributeUsingConsensusAsync(
        WorkerTask task,
        List<AgentInfo> availableAgents,
        CancellationToken cancellationToken)
    {
        // Consensus-based: Agents vote on who should handle the task
        // This is a simplified version - in production, would use distributed consensus algorithm

        if (availableAgents.Count == 0)
            return new List<string>();

        // Simple consensus: agents vote based on their capability match
        var votes = availableAgents
            .Select(agent =>
            {
                var matchScore = CalculateCapabilityMatch(task, agent);
                return new { AgentId = agent.AgentId, Votes = matchScore };
            })
            .OrderByDescending(a => a.Votes)
            .ToList();

        // Select agents with majority votes
        var threshold = votes.Max(a => a.Votes) * 0.7; // 70% of max votes
        return votes
            .Where(a => a.Votes >= threshold)
            .Take(2)
            .Select(a => a.AgentId)
            .ToList();
    }

    private double CalculateFitness(AgentInfo agent, SwarmAgentState? state)
    {
        var baseFitness = 1.0;

        if (state != null)
        {
            // Higher success rate = higher fitness
            baseFitness *= state.SuccessRate;

            // Recent activity bonus
            if (state.LastTaskAssignedAt.HasValue)
            {
                var timeSinceLastTask = DateTimeOffset.UtcNow - state.LastTaskAssignedAt.Value;
                if (timeSinceLastTask < TimeSpan.FromMinutes(5))
                {
                    baseFitness *= 1.2; // 20% bonus for recent activity
                }
            }
        }

        // Capability match bonus
        var capabilityMatch = agent.Capabilities.SupportedTools.Length > 0 ? 1.1 : 1.0;
        baseFitness *= capabilityMatch;

        return baseFitness;
    }

    private double CalculateCapabilityMatch(WorkerTask task, AgentInfo agent)
    {
        if (string.IsNullOrEmpty(task.RequiredCapability))
            return 1.0;

        var hasCapability = agent.Capabilities.SupportedTools
            .Contains(task.RequiredCapability, StringComparer.OrdinalIgnoreCase) ||
            agent.Capabilities.SupportedIntents
            .Contains(task.RequiredCapability, StringComparer.OrdinalIgnoreCase);

        return hasCapability ? 1.0 : 0.3;
    }

    private double CalculateConfidenceScore(List<string> assignedAgents, List<AgentInfo> availableAgents)
    {
        if (assignedAgents.Count == 0 || availableAgents.Count == 0)
            return 0.0;

        lock (_lock)
        {
            var avgSuccessRate = assignedAgents
                .Select(id => _agentStates.GetValueOrDefault(id)?.SuccessRate ?? 0.5)
                .Average();

            var coverage = (double)assignedAgents.Count / availableAgents.Count;

            return (avgSuccessRate * 0.7) + (coverage * 0.3);
        }
    }

    private double CalculateEfficiencyScore()
    {
        lock (_lock)
        {
            if (_agentStates.Count == 0)
                return 0.0;

            var avgSuccessRate = _agentStates.Values.Average(s => s.SuccessRate);
            var taskDistribution = _agentStates.Values
                .Select(s => s.TaskCount)
                .ToList();

            if (taskDistribution.Count == 0)
                return avgSuccessRate;

            // Calculate distribution variance (lower is better)
            var mean = taskDistribution.Average();
            var variance = taskDistribution.Average(x => Math.Pow(x - mean, 2));
            var distributionScore = 1.0 / (1.0 + variance); // Normalize

            return (avgSuccessRate * 0.6) + (distributionScore * 0.4);
        }
    }

    /// <summary>
    /// Records task completion for swarm learning.
    /// </summary>
    /// <param name="agentId">The agent that completed the task.</param>
    /// <param name="success">Whether the task succeeded.</param>
    /// <param name="duration">The task duration.</param>
    internal void RecordTaskCompletion(string agentId, bool success, TimeSpan duration)
    {
        lock (_lock)
        {
            if (_agentStates.TryGetValue(agentId, out var state))
            {
                // Update success rate (exponential moving average)
                var alpha = 0.1; // Learning rate
                state.SuccessRate = (alpha * (success ? 1.0 : 0.0)) + ((1 - alpha) * state.SuccessRate);
                state.LastTaskCompletedAt = DateTimeOffset.UtcNow;
            }

            _taskCompletionTimes.Add(duration);
            if (_taskCompletionTimes.Count > 1000)
            {
                _taskCompletionTimes.RemoveAt(0);
            }
        }
    }
}

/// <summary>
/// State tracking for an agent in a swarm.
/// </summary>
internal class SwarmAgentState
{
    public string AgentId { get; set; } = string.Empty;
    public DateTimeOffset JoinedAt { get; set; }
    public int TaskCount { get; set; }
    public double SuccessRate { get; set; } = 1.0;
    public DateTimeOffset? LastTaskAssignedAt { get; set; }
    public DateTimeOffset? LastTaskCompletedAt { get; set; }
}
