using DotNetAgents.Agents.BehaviorTrees;
using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Supervisor;
using DotNetAgents.Agents.Tasks;
using DotNetAgents.Core.Agents;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DotNetAgents.Agents.Supervisor.BehaviorTrees;

/// <summary>
/// LLM-based task router that uses an LLM to make intelligent routing decisions.
/// </summary>
public class LLMTaskRouter : ITaskRouter
{
    private readonly BehaviorTree<TaskRoutingContext> _tree;
    private readonly BehaviorTreeExecutor<TaskRoutingContext> _executor;
    private readonly AgentExecutor _agentExecutor;
    private readonly ILogger<LLMTaskRouter>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMTaskRouter"/> class.
    /// </summary>
    /// <param name="agentExecutor">The agent executor to use for LLM-based decisions.</param>
    /// <param name="logger">Optional logger instance.</param>
    public LLMTaskRouter(
        AgentExecutor agentExecutor,
        ILogger<LLMTaskRouter>? logger = null)
    {
        _agentExecutor = agentExecutor ?? throw new ArgumentNullException(nameof(agentExecutor));
        _logger = logger;

        // Create logger for behavior tree nodes
        ILogger<BehaviorTreeNode<TaskRoutingContext>>? behaviorTreeLogger = null;
        ILogger<BehaviorTreeExecutor<TaskRoutingContext>>? executorLogger = null;
        if (logger != null)
        {
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug));
            behaviorTreeLogger = loggerFactory.CreateLogger<BehaviorTreeNode<TaskRoutingContext>>();
            executorLogger = loggerFactory.CreateLogger<BehaviorTreeExecutor<TaskRoutingContext>>();
        }

        // Build behavior tree with LLM-based decision making
        var root = new SequenceNode<TaskRoutingContext>("LLMRouter", behaviorTreeLogger)
            .AddChild(new LLMActionNode<TaskRoutingContext>(
                "LLMRoutingDecision",
                _agentExecutor,
                contextToPrompt: BuildRoutingPrompt,
                resultToContext: ParseRoutingResult,
                behaviorTreeLogger))
            .AddChild(new ActionNode<TaskRoutingContext>(
                "ApplyRoutingDecision",
                async (ctx, ct) =>
                {
                    if (ctx.SelectedWorker != null)
                    {
                        _logger?.LogInformation(
                            "LLM selected worker {WorkerId} using strategy {Strategy} for task {TaskId}",
                            ctx.SelectedWorker.AgentId,
                            ctx.RoutingStrategy,
                            ctx.Task.TaskId);
                        return BehaviorTreeNodeStatus.Success;
                    }
                    return BehaviorTreeNodeStatus.Failure;
                },
                behaviorTreeLogger));

        _tree = new BehaviorTree<TaskRoutingContext>("LLMTaskRoutingTree", root);
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

        if (availableWorkers.Count == 0)
        {
            _logger?.LogWarning("No available workers for task {TaskId}", task.TaskId);
            return null;
        }

        var context = new TaskRoutingContext
        {
            Task = task,
            AvailableWorkers = availableWorkers
        };

        var result = await _executor.ExecuteAsync(_tree, context, cancellationToken).ConfigureAwait(false);

        if (result.Status == BehaviorTreeNodeStatus.Success && context.SelectedWorker != null)
        {
            return context.SelectedWorker;
        }

        _logger?.LogWarning("LLM routing failed for task {TaskId}, falling back to first available worker", task.TaskId);

        // Fallback to first available worker
        return availableWorkers.FirstOrDefault();
    }

    /// <summary>
    /// Builds a prompt for the LLM to make routing decisions.
    /// </summary>
    private string BuildRoutingPrompt(TaskRoutingContext context)
    {
        var workersInfo = context.AvailableWorkers.Select((w, idx) => new
        {
            Index = idx,
            WorkerId = w.AgentId,
            AgentType = w.AgentType,
            Status = w.Status.ToString(),
            CurrentTaskCount = w.CurrentTaskCount,
            MaxConcurrentTasks = w.Capabilities.MaxConcurrentTasks,
            SupportedTools = string.Join(", ", w.Capabilities.SupportedTools),
            SupportedIntents = string.Join(", ", w.Capabilities.SupportedIntents),
            LoadPercentage = w.Capabilities.MaxConcurrentTasks > 0
                ? (double)w.CurrentTaskCount / w.Capabilities.MaxConcurrentTasks * 100
                : 0
        }).ToList();

        var prompt = $@"You are a task routing system for a multi-agent framework. Your job is to select the best worker agent for a given task.

TASK TO ROUTE:
- Task ID: {context.Task.TaskId}
- Task Type: {context.Task.TaskType}
- Priority: {context.Task.Priority} (1-10, higher is more important)
- Required Capability: {context.Task.RequiredCapability ?? "None specified"}
- Input: {JsonSerializer.Serialize(context.Task.Input)}
- Metadata: {JsonSerializer.Serialize(context.Task.Metadata)}
- Preferred Agent ID: {context.Task.PreferredAgentId ?? "None"}
- Timeout: {context.Task.Timeout?.ToString() ?? "None"}

AVAILABLE WORKERS:
{JsonSerializer.Serialize(workersInfo, new JsonSerializerOptions { WriteIndented = true })}

ROUTING CRITERIA:
1. Match required capability if specified
2. Consider worker load (lower is better)
3. Consider task priority (high priority tasks should go to less loaded workers)
4. Consider worker capabilities and specializations
5. Balance load across workers

RESPONSE FORMAT (JSON):
{{
  ""selectedWorkerIndex"": <integer index from AvailableWorkers array>,
  ""reasoning"": ""<brief explanation of why this worker was selected>"",
  ""routingStrategy"": ""<strategy name: CapabilityMatch, LoadBalance, PriorityBased, etc>""
}}

Respond ONLY with valid JSON in the format above. Do not include any other text.";

        return prompt;
    }

    /// <summary>
    /// Parses the LLM routing result and updates the context.
    /// </summary>
    private TaskRoutingContext ParseRoutingResult(string llmResult, TaskRoutingContext context)
    {
        try
        {
            // Try to extract JSON from the response (LLM might include markdown code blocks)
            var jsonStart = llmResult.IndexOf('{');
            var jsonEnd = llmResult.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = llmResult.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var routingDecision = JsonSerializer.Deserialize<RoutingDecision>(json);

                if (routingDecision != null &&
                    routingDecision.SelectedWorkerIndex >= 0 &&
                    routingDecision.SelectedWorkerIndex < context.AvailableWorkers.Count)
                {
                    context.SelectedWorker = context.AvailableWorkers[routingDecision.SelectedWorkerIndex];
                    context.RoutingStrategy = routingDecision.RoutingStrategy ?? "LLMBased";

                    _logger?.LogDebug(
                        "LLM routing decision: Worker {WorkerId}, Strategy: {Strategy}, Reasoning: {Reasoning}",
                        context.SelectedWorker.AgentId,
                        context.RoutingStrategy,
                        routingDecision.Reasoning);
                }
                else
                {
                    _logger?.LogWarning(
                        "Invalid worker index {Index} from LLM (valid range: 0-{Max})",
                        routingDecision?.SelectedWorkerIndex ?? -1,
                        context.AvailableWorkers.Count - 1);
                }
            }
            else
            {
                _logger?.LogWarning("Could not parse JSON from LLM response: {Response}", llmResult);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to parse LLM routing result: {Result}", llmResult);
        }

        return context;
    }

    /// <summary>
    /// Represents the routing decision from the LLM.
    /// </summary>
    private class RoutingDecision
    {
        public int SelectedWorkerIndex { get; set; }
        public string? Reasoning { get; set; }
        public string? RoutingStrategy { get; set; }
    }
}
