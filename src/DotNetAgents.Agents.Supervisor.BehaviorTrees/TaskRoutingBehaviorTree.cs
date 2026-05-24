// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.BehaviorTrees;
using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Supervisor;
using DotNetAgents.Agents.Tasks;
using DotNetAgents.Agents.WorkerPool;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Supervisor.BehaviorTrees;

/// <summary>
/// Context for task routing behavior tree decisions.
/// </summary>
public class TaskRoutingContext
{
    /// <summary>
    /// Gets or sets the task to route.
    /// </summary>
    public WorkerTask Task { get; set; } = null!;

    /// <summary>
    /// Gets or sets the available workers.
    /// </summary>
    public IReadOnlyList<AgentInfo> AvailableWorkers { get; set; } = Array.Empty<AgentInfo>();

    /// <summary>
    /// Gets or sets the selected worker (set by behavior tree).
    /// </summary>
    public AgentInfo? SelectedWorker { get; set; }

    /// <summary>
    /// Gets or sets the routing strategy used (set by behavior tree).
    /// </summary>
    public string? RoutingStrategy { get; set; }
}

/// <summary>
/// Behavior tree for intelligent task routing decisions.
/// Implements ITaskRouter to be used with SupervisorAgent.
/// </summary>
public class TaskRoutingBehaviorTree : ITaskRouter
{
    private readonly BehaviorTree<TaskRoutingContext> _tree;
    private readonly BehaviorTreeExecutor<TaskRoutingContext> _executor;
    private readonly IWorkerPool _workerPool;
    private readonly int _highPriorityThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRoutingBehaviorTree"/> class.
    /// </summary>
    /// <param name="workerPool">The worker pool to select workers from.</param>
    /// <param name="highPriorityThreshold">Priority threshold for high-priority tasks (default: 7).</param>
    /// <param name="logger">Optional logger instance.</param>
    public TaskRoutingBehaviorTree(
        IWorkerPool workerPool,
        int highPriorityThreshold = 7,
        ILogger<TaskRoutingBehaviorTree>? logger = null)
    {
        _workerPool = workerPool ?? throw new ArgumentNullException(nameof(workerPool));
        _highPriorityThreshold = highPriorityThreshold;

        ILogger<BehaviorTreeNode<TaskRoutingContext>>? behaviorTreeLogger = null;
        if (logger != null)
        {
            // Create a logger for behavior tree nodes from the TaskRoutingBehaviorTree logger
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
            behaviorTreeLogger = loggerFactory.CreateLogger<BehaviorTreeNode<TaskRoutingContext>>();
        }

        // Build behavior tree: Selector (try strategies in order)
        var root = new SelectorNode<TaskRoutingContext>("TaskRouter", behaviorTreeLogger)
            // Strategy 1: High Priority Task - Route to priority-based worker
            .AddChild(new SequenceNode<TaskRoutingContext>("HighPriorityRoute", behaviorTreeLogger)
                .AddChild(new ConditionNode<TaskRoutingContext>(
                    "IsHighPriority",
                    ctx => ctx.Task.Priority >= _highPriorityThreshold,
                    behaviorTreeLogger))
                .AddChild(new ActionNode<TaskRoutingContext>(
                    "SelectPriorityWorker",
                    async (ctx, ct) =>
                    {
                        // Find worker with matching capability and lowest current task count
                        var matchingWorkers = ctx.AvailableWorkers
                            .Where(w => string.IsNullOrEmpty(ctx.Task.RequiredCapability) ||
                                       w.Capabilities.SupportedTools.Contains(ctx.Task.RequiredCapability) ||
                                       w.Capabilities.SupportedIntents.Contains(ctx.Task.RequiredCapability))
                            .OrderBy(w => w.CurrentTaskCount)
                            .ToList();

                        if (matchingWorkers.Count > 0)
                        {
                            ctx.SelectedWorker = matchingWorkers[0];
                            ctx.RoutingStrategy = "PriorityBased";
                            return BehaviorTreeNodeStatus.Success;
                        }

                        return BehaviorTreeNodeStatus.Failure;
                    },
                    behaviorTreeLogger)))
            // Strategy 2: Capability Match - Route to worker with exact capability
            .AddChild(new SequenceNode<TaskRoutingContext>("CapabilityMatchRoute", behaviorTreeLogger)
                .AddChild(new ConditionNode<TaskRoutingContext>(
                    "HasRequiredCapability",
                    ctx => !string.IsNullOrEmpty(ctx.Task.RequiredCapability),
                    behaviorTreeLogger))
                .AddChild(new ActionNode<TaskRoutingContext>(
                    "SelectCapabilityWorker",
                    async (ctx, ct) =>
                    {
                        var matchingWorker = ctx.AvailableWorkers
                            .FirstOrDefault(w => w.Capabilities.SupportedTools.Contains(ctx.Task.RequiredCapability!) ||
                                                w.Capabilities.SupportedIntents.Contains(ctx.Task.RequiredCapability!));

                        if (matchingWorker != null)
                        {
                            ctx.SelectedWorker = matchingWorker;
                            ctx.RoutingStrategy = "CapabilityBased";
                            return BehaviorTreeNodeStatus.Success;
                        }

                        return BehaviorTreeNodeStatus.Failure;
                    },
                    behaviorTreeLogger)))
            // Strategy 3: Load Balance - Route using load balancing
            .AddChild(new SequenceNode<TaskRoutingContext>("LoadBalanceRoute", behaviorTreeLogger)
                .AddChild(new ConditionNode<TaskRoutingContext>(
                    "HasMultipleWorkers",
                    ctx => ctx.AvailableWorkers.Count > 1,
                    behaviorTreeLogger))
                .AddChild(new ActionNode<TaskRoutingContext>(
                    "SelectLoadBalancedWorker",
                    async (ctx, ct) =>
                    {
                        // Use worker pool's load balancing
                        var worker = await _workerPool.GetAvailableWorkerAsync(
                            ctx.Task.RequiredCapability,
                            ct).ConfigureAwait(false);

                        if (worker != null)
                        {
                            ctx.SelectedWorker = worker;
                            ctx.RoutingStrategy = "LoadBalanced";
                            return BehaviorTreeNodeStatus.Success;
                        }

                        return BehaviorTreeNodeStatus.Failure;
                    },
                    behaviorTreeLogger)))
            // Strategy 4: Fallback - Route to any available worker
            .AddChild(new ActionNode<TaskRoutingContext>(
                "FallbackRoute",
                async (ctx, ct) =>
                {
                    var worker = await _workerPool.GetAvailableWorkerAsync(
                        requiredCapability: null,
                        ct).ConfigureAwait(false);

                    if (worker != null)
                    {
                        ctx.SelectedWorker = worker;
                        ctx.RoutingStrategy = "Fallback";
                        return BehaviorTreeNodeStatus.Success;
                    }

                    return BehaviorTreeNodeStatus.Failure;
                },
                behaviorTreeLogger));

        _tree = new BehaviorTree<TaskRoutingContext>("TaskRoutingTree", root);

        // Create executor logger
        ILogger<BehaviorTreeExecutor<TaskRoutingContext>>? executorLogger = null;
        if (logger != null)
        {
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug));
            executorLogger = loggerFactory.CreateLogger<BehaviorTreeExecutor<TaskRoutingContext>>();
        }

        _executor = new BehaviorTreeExecutor<TaskRoutingContext>(executorLogger);
    }

    /// <inheritdoc/>
    public async Task<AgentInfo?> RouteTaskAsync(
        WorkerTask task,
        IReadOnlyList<AgentInfo> availableWorkers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(availableWorkers);

        var context = new TaskRoutingContext
        {
            Task = task,
            AvailableWorkers = availableWorkers
        };

        var result = await _executor.ExecuteAsync(_tree, context, cancellationToken).ConfigureAwait(false);

        if (result.Status == BehaviorTreeNodeStatus.Success)
        {
            return context.SelectedWorker;
        }

        return null;
    }
}
