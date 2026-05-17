using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;
using DotNetAgents.Agents.WorkerPool.AutoScaling;
using DotNetAgents.Agents.WorkerPool.LoadBalancing;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.WorkerPool;

/// <summary>
/// Basic implementation of <see cref="IWorkerPool"/>.
/// </summary>
public class WorkerPool : IWorkerPool
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILoadBalancer _loadBalancer;
    private readonly IAutoScaler? _autoScaler;
    private readonly ITaskQueue? _taskQueue;
    private readonly LoadBalancingStrategy _defaultStrategy;
    private readonly ILogger<WorkerPool>? _logger;
    private readonly HashSet<string> _workerIds = new();
    private readonly object _lock = new();
    private int _totalTasksProcessed;
    private readonly Dictionary<string, int> _tasksByWorker = new();
    private readonly List<TimeSpan> _taskDurations = new();
    private readonly IWorkerStateProvider? _stateProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkerPool"/> class.
    /// </summary>
    /// <param name="agentRegistry">The agent registry.</param>
    /// <param name="loadBalancer">Optional load balancer. If null, uses default <see cref="LoadBalancer"/>.</param>
    /// <param name="autoScaler">Optional auto-scaler. If null, auto-scaling is disabled.</param>
    /// <param name="taskQueue">Optional task queue. If provided, enables accurate pending task count for auto-scaling.</param>
    /// <param name="defaultStrategy">The default load balancing strategy to use.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="stateProvider">Optional state provider for state-based worker selection.</param>
    public WorkerPool(
        IAgentRegistry agentRegistry,
        ILoadBalancer? loadBalancer = null,
        IAutoScaler? autoScaler = null,
        ITaskQueue? taskQueue = null,
        LoadBalancingStrategy defaultStrategy = LoadBalancingStrategy.PriorityBased,
        ILogger<WorkerPool>? logger = null,
        IWorkerStateProvider? stateProvider = null)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _loadBalancer = loadBalancer ?? new LoadBalancer();
        _autoScaler = autoScaler;
        _taskQueue = taskQueue;
        _defaultStrategy = defaultStrategy;
        _logger = logger;
        _stateProvider = stateProvider;
    }

    /// <inheritdoc />
    public int WorkerCount
    {
        get
        {
            lock (_lock)
            {
                return _workerIds.Count;
            }
        }
    }

    /// <inheritdoc />
    public int AvailableWorkerCount
    {
        get
        {
            lock (_lock)
            {
                return _workerIds.Count(id =>
                {
                    var agentInfo = _agentRegistry.GetByIdAsync(id).GetAwaiter().GetResult();
                    if (agentInfo == null)
                    {
                        return false;
                    }

                    // Use state machine if available, otherwise fall back to AgentStatus enum
                    if (_stateProvider != null)
                    {
                        var currentState = _stateProvider.GetAgentState(id);
                        return AgentStatusStateMachineAdapter.IsAvailable(
                            currentState,
                            agentInfo.CurrentTaskCount,
                            agentInfo.Capabilities.MaxConcurrentTasks);
                    }

                    return agentInfo.Status == AgentStatus.Available &&
                           agentInfo.CurrentTaskCount < agentInfo.Capabilities.MaxConcurrentTasks;
                });
            }
        }
    }

    /// <inheritdoc />
    public async Task AddWorkerAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        var agentInfo = await _agentRegistry.GetByIdAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (agentInfo == null)
        {
            throw new InvalidOperationException($"Agent {agentId} is not registered.");
        }

        lock (_lock)
        {
            if (_workerIds.Add(agentId))
            {
                _logger?.LogInformation("Added worker {AgentId} to pool", agentId);
            }
            else
            {
                _logger?.LogWarning("Worker {AgentId} is already in the pool", agentId);
            }
        }
    }

    /// <inheritdoc />
    public Task RemoveWorkerAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_workerIds.Remove(agentId))
            {
                _logger?.LogInformation("Removed worker {AgentId} from pool", agentId);
            }
            else
            {
                _logger?.LogWarning("Worker {AgentId} was not in the pool", agentId);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<AgentInfo?> GetAvailableWorkerAsync(
        string? requiredCapability = null,
        CancellationToken cancellationToken = default)
    {
        return await GetAvailableWorkerAsync(null, requiredCapability, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an available worker for a task using the specified load balancing strategy.
    /// </summary>
    /// <param name="task">The task to assign (optional, used for capability-based selection).</param>
    /// <param name="requiredCapability">Optional required capability for the task.</param>
    /// <param name="strategy">Optional load balancing strategy. If null, uses the default strategy.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An available worker agent, or null if none available.</returns>
    public async Task<AgentInfo?> GetAvailableWorkerAsync(
        WorkerTask? task = null,
        string? requiredCapability = null,
        LoadBalancingStrategy? strategy = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AgentInfo> candidates;

        lock (_lock)
        {
            var workerIds = _workerIds.ToList();
            if (workerIds.Count == 0)
            {
                return null;
            }

            // Get all workers from registry
            var allWorkers = _agentRegistry.GetAllAsync(cancellationToken).GetAwaiter().GetResult();

            // Filter workers - use state machine if available, otherwise use AgentStatus enum
            candidates = allWorkers
                .Where(agent => workerIds.Contains(agent.AgentId))
                .Where(agent =>
                {
                    if (_stateProvider != null)
                    {
                        var currentState = _stateProvider.GetAgentState(agent.AgentId);
                        return AgentStatusStateMachineAdapter.IsAvailable(
                            currentState,
                            agent.CurrentTaskCount,
                            agent.Capabilities.MaxConcurrentTasks);
                    }

                    // Fallback to enum-based check
                    return agent.Status == AgentStatus.Available &&
                           agent.CurrentTaskCount < agent.Capabilities.MaxConcurrentTasks;
                })
                .ToList();
        }

        // Filter by capability if specified
        var capability = requiredCapability ?? task?.RequiredCapability;
        if (!string.IsNullOrEmpty(capability))
        {
            candidates = candidates
                .Where(agent => agent.Capabilities.SupportedTools.Contains(capability, StringComparer.OrdinalIgnoreCase) ||
                               agent.Capabilities.SupportedIntents.Contains(capability, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        // Use load balancer to select worker
        var taskForSelection = task ?? (capability != null
            ? new WorkerTask { RequiredCapability = capability }
            : new WorkerTask());

        var selected = _loadBalancer.SelectWorker(
            candidates,
            taskForSelection,
            strategy ?? _defaultStrategy);

        return selected;
    }

    /// <inheritdoc />
    public Task<WorkerPoolStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var allWorkers = _agentRegistry.GetAllAsync(cancellationToken).GetAwaiter().GetResult();
            var poolWorkers = allWorkers.Where(w => _workerIds.Contains(w.AgentId)).ToList();

            var availableWorkers = poolWorkers.Count(w =>
            {
                if (_stateProvider != null)
                {
                    var currentState = _stateProvider.GetAgentState(w.AgentId);
                    return AgentStatusStateMachineAdapter.IsAvailable(
                        currentState,
                        w.CurrentTaskCount,
                        w.Capabilities.MaxConcurrentTasks);
                }

                return w.Status == AgentStatus.Available &&
                       w.CurrentTaskCount < w.Capabilities.MaxConcurrentTasks;
            });

            var busyWorkers = poolWorkers.Count(w =>
            {
                if (_stateProvider != null)
                {
                    var currentState = _stateProvider.GetAgentState(w.AgentId);
                    var status = !string.IsNullOrEmpty(currentState)
                        ? AgentStatusStateMachineAdapter.GetStatusFromStateMachine(currentState)
                        : w.Status;
                    return status == AgentStatus.Busy ||
                           w.CurrentTaskCount >= w.Capabilities.MaxConcurrentTasks;
                }

                return w.Status == AgentStatus.Busy ||
                       w.CurrentTaskCount >= w.Capabilities.MaxConcurrentTasks;
            });

            var averageDuration = _taskDurations.Count > 0
                ? TimeSpan.FromMilliseconds(_taskDurations.Average(d => d.TotalMilliseconds))
                : TimeSpan.Zero;

            return Task.FromResult(new WorkerPoolStatistics
            {
                TotalWorkers = _workerIds.Count,
                AvailableWorkers = availableWorkers,
                BusyWorkers = busyWorkers,
                TotalTasksProcessed = _totalTasksProcessed,
                AverageTaskDuration = averageDuration,
                TasksByWorker = new Dictionary<string, int>(_tasksByWorker)
            });
        }
    }

    /// <inheritdoc />
    public async Task<AutoScaling.ScalingDecision> EvaluateAutoScalingAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_autoScaler == null)
        {
            return new AutoScaling.ScalingDecision
            {
                Action = AutoScaling.ScalingAction.None,
                WorkerCount = 0,
                Reason = "Auto-scaling is not enabled"
            };
        }

        lock (_lock)
        {
            var allWorkers = _agentRegistry.GetAllAsync(cancellationToken).GetAwaiter().GetResult();
            var poolWorkers = allWorkers.Where(w => _workerIds.Contains(w.AgentId)).ToList();

            // Get pending task count from task queue if available
            var pendingTaskCount = _taskQueue != null
                ? _taskQueue.GetPendingCountAsync(cancellationToken).GetAwaiter().GetResult()
                : 0;

            var averageDuration = _taskDurations.Count > 0
                ? TimeSpan.FromMilliseconds(_taskDurations.Average(d => d.TotalMilliseconds))
                : TimeSpan.Zero;

            return _autoScaler.EvaluateScalingAsync(
                poolWorkers,
                pendingTaskCount,
                averageDuration,
                cancellationToken).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Records a completed task for statistics.
    /// </summary>
    /// <param name="agentId">The ID of the agent that completed the task.</param>
    /// <param name="duration">The duration of the task.</param>
    internal void RecordTaskCompletion(string agentId, TimeSpan duration)
    {
        lock (_lock)
        {
            _totalTasksProcessed++;
            _tasksByWorker.TryGetValue(agentId, out var count);
            _tasksByWorker[agentId] = count + 1;
            _taskDurations.Add(duration);

            // Keep only last 1000 durations for memory efficiency
            if (_taskDurations.Count > 1000)
            {
                _taskDurations.RemoveAt(0);
            }
        }
    }
}
