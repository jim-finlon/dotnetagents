// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.Messaging;
using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;
using DotNetAgents.Agents.WorkerPool;
using Microsoft.Extensions.Logging;
using TaskStatus = DotNetAgents.Agents.Tasks.TaskStatus;

namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Implementation of <see cref="ISupervisorAgent"/> that delegates tasks to worker agents.
/// </summary>
public class SupervisorAgent : ISupervisorAgent
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IAgentMessageBus _messageBus;
    private readonly ITaskQueue _taskQueue;
    private readonly ITaskStore _taskStore;
    private readonly IWorkerPool _workerPool;
    private readonly ILogger<SupervisorAgent>? _logger;
    private readonly Dictionary<string, DateTimeOffset> _taskStartTimes = new();
    private readonly Dictionary<string, TimeSpan> _taskExecutionTimes = new();
    private readonly Dictionary<string, int> _tasksByType = new();
    private readonly Dictionary<string, int> _tasksByAgent = new();
    private int _totalTasksSubmitted;
    private int _tasksCompleted;
    private int _tasksFailed;
    private readonly object _statsLock = new();
    private readonly ISupervisorStateMachine<SupervisorContext>? _stateMachine;
    private readonly SupervisorContext _context;
    private readonly ITaskRouter? _taskRouter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SupervisorAgent"/> class.
    /// </summary>
    /// <param name="agentRegistry">The agent registry.</param>
    /// <param name="messageBus">The message bus for agent communication.</param>
    /// <param name="taskQueue">The task queue.</param>
    /// <param name="taskStore">The task store.</param>
    /// <param name="workerPool">The worker pool.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="stateMachine">Optional state machine for supervisor lifecycle management.</param>
    /// <param name="taskRouter">Optional task router for intelligent task routing (e.g., behavior tree-based).</param>
    public SupervisorAgent(
        IAgentRegistry agentRegistry,
        IAgentMessageBus messageBus,
        ITaskQueue taskQueue,
        ITaskStore taskStore,
        IWorkerPool workerPool,
        ILogger<SupervisorAgent>? logger = null,
        ISupervisorStateMachine<SupervisorContext>? stateMachine = null,
        ITaskRouter? taskRouter = null)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));
        _workerPool = workerPool ?? throw new ArgumentNullException(nameof(workerPool));
        _logger = logger;
        _stateMachine = stateMachine;
        _context = new SupervisorContext { SupervisorId = "supervisor" };
        _taskRouter = taskRouter;

        // Initialize state machine if provided
        if (_stateMachine != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _stateMachine.TransitionAsync("Monitoring", _context, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize supervisor state machine");
                }
            });
        }

        // Subscribe to task result messages
        _ = Task.Run(async () =>
        {
            await _messageBus.SubscribeByTypeAsync("task_result", HandleTaskResultAsync, CancellationToken.None)
                .ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public async Task<string> SubmitTaskAsync(
        WorkerTask task,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        cancellationToken.ThrowIfCancellationRequested();

        // Save task to store
        await _taskStore.SaveAsync(task, cancellationToken).ConfigureAwait(false);

        // Enqueue task
        await _taskQueue.EnqueueAsync(task, cancellationToken).ConfigureAwait(false);

        lock (_statsLock)
        {
            _totalTasksSubmitted++;
            _tasksByType.TryGetValue(task.TaskType, out var count);
            _tasksByType[task.TaskType] = count + 1;
        }

        _logger?.LogInformation(
            "Submitted task {TaskId} of type {TaskType}",
            task.TaskId,
            task.TaskType);

        // Update context and transition state machine
        if (_stateMachine != null)
        {
            lock (_statsLock)
            {
                _context.PendingTasks = _taskQueue.GetPendingCountAsync(cancellationToken).GetAwaiter().GetResult() + 1;
                _context.CurrentTaskCount = _taskStartTimes.Count;
            }

            try
            {
                if (_stateMachine.CurrentState == "Monitoring")
                {
                    await _stateMachine.TransitionAsync("Analyzing", _context, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to transition state machine to Analyzing");
            }
        }

        // Try to assign immediately if workers are available
        _ = Task.Run(() => TryAssignTasksAsync(cancellationToken), cancellationToken);

        return task.TaskId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> SubmitTasksAsync(
        IEnumerable<WorkerTask> tasks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        cancellationToken.ThrowIfCancellationRequested();

        var taskIds = new List<string>();
        foreach (var task in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var taskId = await SubmitTaskAsync(task, cancellationToken).ConfigureAwait(false);
            taskIds.Add(taskId);
        }

        return taskIds;
    }

    /// <inheritdoc />
    public async Task<TaskStatus> GetTaskStatusAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        cancellationToken.ThrowIfCancellationRequested();

        var task = await _taskStore.GetAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
        {
            return TaskStatus.Pending;
        }

        // Check if result exists (completed or failed)
        var result = await _taskStore.GetResultAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (result != null)
        {
            return result.Success ? TaskStatus.Completed : TaskStatus.Failed;
        }

        // Check if task is in progress
        lock (_statsLock)
        {
            if (_taskStartTimes.ContainsKey(taskId))
            {
                return TaskStatus.InProgress;
            }
        }

        return TaskStatus.Pending;
    }

    /// <inheritdoc />
    public async Task<WorkerTaskResult?> GetTaskResultAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        cancellationToken.ThrowIfCancellationRequested();

        return await _taskStore.GetResultAsync(taskId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CancelTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        cancellationToken.ThrowIfCancellationRequested();

        var status = await GetTaskStatusAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (status == TaskStatus.Completed || status == TaskStatus.Failed)
        {
            return false;
        }

        await _taskStore.UpdateStatusAsync(taskId, TaskStatus.Cancelled, cancellationToken)
            .ConfigureAwait(false);

        _logger?.LogInformation("Cancelled task {TaskId}", taskId);
        return true;
    }

    /// <inheritdoc />
    public Task<SupervisorStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_statsLock)
        {
            var pendingCount = _taskQueue.GetPendingCountAsync(cancellationToken).GetAwaiter().GetResult();
            var inProgressCount = _taskStartTimes.Count;

            var averageExecutionTime = _taskExecutionTimes.Count > 0
                ? TimeSpan.FromMilliseconds(_taskExecutionTimes.Values.Average(t => t.TotalMilliseconds))
                : TimeSpan.Zero;

            var stats = new SupervisorStatistics
            {
                TotalTasksSubmitted = _totalTasksSubmitted,
                TasksCompleted = _tasksCompleted,
                TasksFailed = _tasksFailed,
                TasksPending = pendingCount,
                TasksInProgress = inProgressCount,
                AverageExecutionTime = averageExecutionTime,
                TasksByType = new Dictionary<string, int>(_tasksByType),
                TasksByAgent = new Dictionary<string, int>(_tasksByAgent),
                CurrentState = _stateMachine?.CurrentState
            };

            return Task.FromResult(stats);
        }
    }

    /// <summary>
    /// Gets the current state machine state, if state machine is enabled.
    /// </summary>
    /// <returns>The current state, or null if state machine is not enabled.</returns>
    public string? GetCurrentState()
    {
        return _stateMachine?.CurrentState;
    }

    private async Task TryAssignTasksAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Update context
                if (_stateMachine != null)
                {
                    lock (_statsLock)
                    {
                        _context.PendingTasks = _taskQueue.GetPendingCountAsync(cancellationToken).GetAwaiter().GetResult();
                        _context.CurrentTaskCount = _taskStartTimes.Count;
                        _context.AvailableWorkers = _workerPool.AvailableWorkerCount;
                    }

                    // Transition to Analyzing if in Monitoring and tasks are pending
                    if (_stateMachine.CurrentState == "Monitoring" && _context.PendingTasks > 0)
                    {
                        try
                        {
                            await _stateMachine.TransitionAsync("Analyzing", _context, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to transition to Analyzing state");
                        }
                    }
                }

                var task = await _taskQueue.DequeueAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (task == null)
                {
                    // Transition back to Monitoring if no tasks
                    if (_stateMachine != null && _stateMachine.CurrentState != "Monitoring")
                    {
                        try
                        {
                            await _stateMachine.TransitionAsync("Monitoring", _context, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to transition to Monitoring state");
                        }
                    }

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Transition to Delegating
                if (_stateMachine != null && _stateMachine.CurrentState == "Analyzing")
                {
                    try
                    {
                        await _stateMachine.TransitionAsync("Delegating", _context, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to transition to Delegating state");
                    }
                }

                // Find available worker using task router or standard method
                AgentInfo? worker;
                if (_taskRouter != null)
                {
                    // Get available workers from registry for intelligent routing
                    var availableWorkersList = new List<AgentInfo>();

                    // Get workers from registry that match capability
                    var allAgents = await _agentRegistry.FindByCapabilityAsync(
                        task.RequiredCapability ?? string.Empty,
                        cancellationToken).ConfigureAwait(false);

                    // Filter to available workers
                    foreach (var agent in allAgents)
                    {
                        if (agent.Status == AgentStatus.Available &&
                            agent.CurrentTaskCount < agent.Capabilities.MaxConcurrentTasks)
                        {
                            availableWorkersList.Add(agent);
                        }
                    }

                    if (availableWorkersList.Count > 0)
                    {
                        worker = await _taskRouter.RouteTaskAsync(task, availableWorkersList, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        // Fallback to standard method if no workers found
                        worker = await _workerPool.GetAvailableWorkerAsync(
                            task.RequiredCapability,
                            cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Standard routing
                    worker = await _workerPool.GetAvailableWorkerAsync(
                        task.RequiredCapability,
                        cancellationToken).ConfigureAwait(false);
                }

                if (worker == null)
                {
                    // No worker available, transition to Waiting and re-enqueue task
                    if (_stateMachine != null && _stateMachine.CurrentState == "Delegating")
                    {
                        try
                        {
                            await _stateMachine.TransitionAsync("Waiting", _context, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to transition to Waiting state");
                        }
                    }

                    await _taskQueue.EnqueueAsync(task, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Assign task to worker
                await AssignTaskToWorkerAsync(task, worker, cancellationToken).ConfigureAwait(false);

                // Transition to Waiting after delegation
                if (_stateMachine != null && _stateMachine.CurrentState == "Delegating")
                {
                    try
                    {
                        _context.LastDelegationTime = DateTimeOffset.UtcNow;
                        await _stateMachine.TransitionAsync("Waiting", _context, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to transition to Waiting state");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in task assignment loop");

            // Transition to Error state
            if (_stateMachine != null)
            {
                try
                {
                    lock (_statsLock)
                    {
                        _context.ErrorCount++;
                        _context.LastErrorMessage = ex.Message;
                    }
                    await _stateMachine.TransitionAsync("Error", _context, cancellationToken)
                        .ConfigureAwait(false);

                    // Recover from error after a delay
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    await _stateMachine.TransitionAsync("Monitoring", _context, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception recoveryEx)
                {
                    _logger?.LogError(recoveryEx, "Failed to recover from error state");
                }
            }
        }
    }

    private async Task AssignTaskToWorkerAsync(
        WorkerTask task,
        AgentInfo worker,
        CancellationToken cancellationToken)
    {
        lock (_statsLock)
        {
            _taskStartTimes[task.TaskId] = DateTimeOffset.UtcNow;
            _tasksByAgent.TryGetValue(worker.AgentId, out var count);
            _tasksByAgent[worker.AgentId] = count + 1;
        }

        await _taskStore.UpdateStatusAsync(task.TaskId, TaskStatus.Assigned, cancellationToken)
            .ConfigureAwait(false);

        await _agentRegistry.UpdateTaskCountAsync(worker.AgentId, worker.CurrentTaskCount + 1, cancellationToken)
            .ConfigureAwait(false);

        await _agentRegistry.UpdateStatusAsync(worker.AgentId, AgentStatus.Busy, cancellationToken)
            .ConfigureAwait(false);

        // Send task to worker via message bus
        var message = new AgentMessage
        {
            FromAgentId = "supervisor",
            ToAgentId = worker.AgentId,
            MessageType = "task_assignment",
            Payload = task,
            CorrelationId = task.TaskId
        };

        await _messageBus.SendAsync(message, cancellationToken).ConfigureAwait(false);

        _logger?.LogInformation(
            "Assigned task {TaskId} to worker {WorkerId}",
            task.TaskId,
            worker.AgentId);
    }

    private async Task HandleTaskResultAsync(
        AgentMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Payload is not WorkerTaskResult result)
        {
            _logger?.LogWarning("Received task result message with invalid payload");
            return;
        }

        await _taskStore.SaveResultAsync(result, cancellationToken).ConfigureAwait(false);

        lock (_statsLock)
        {
            if (_taskStartTimes.TryGetValue(result.TaskId, out var startTime))
            {
                var executionTime = result.CompletedAt - startTime;
                _taskExecutionTimes[result.TaskId] = executionTime;
                _taskStartTimes.Remove(result.TaskId);
            }

            if (result.Success)
            {
                _tasksCompleted++;
            }
            else
            {
                _tasksFailed++;
            }
        }

        // Update worker status
        var worker = await _agentRegistry.GetByIdAsync(result.WorkerAgentId, cancellationToken)
            .ConfigureAwait(false);
        if (worker != null)
        {
            var newTaskCount = Math.Max(0, worker.CurrentTaskCount - 1);
            await _agentRegistry.UpdateTaskCountAsync(result.WorkerAgentId, newTaskCount, cancellationToken)
                .ConfigureAwait(false);

            var newStatus = newTaskCount == 0 ? AgentStatus.Available : AgentStatus.Busy;
            await _agentRegistry.UpdateStatusAsync(result.WorkerAgentId, newStatus, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger?.LogInformation(
            "Task {TaskId} completed by worker {WorkerId} (Success: {Success})",
            result.TaskId,
            result.WorkerAgentId,
            result.Success);

        // Transition from Waiting back to Monitoring when results arrive
        if (_stateMachine != null && _stateMachine.CurrentState == "Waiting")
        {
            try
            {
                lock (_statsLock)
                {
                    _context.PendingTasks = _taskQueue.GetPendingCountAsync(cancellationToken).GetAwaiter().GetResult();
                    _context.CurrentTaskCount = _taskStartTimes.Count;
                }

                await _stateMachine.TransitionAsync("Monitoring", _context, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to transition from Waiting to Monitoring");
            }
        }
    }
}
