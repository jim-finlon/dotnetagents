using DotNetAgents.Agents.BehaviorTrees;
using DotNetAgents.Voice.IntentClassification;
using DotNetAgents.Voice.Orchestration;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.BehaviorTrees;

/// <summary>
/// Behavior tree for intelligent command processing that determines the appropriate processing strategy
/// based on command complexity, intent confidence, and completeness.
/// </summary>
public class CommandProcessingBehaviorTree
{
    private readonly BehaviorTree<CommandProcessingContext> _tree;
    private readonly BehaviorTreeExecutor<CommandProcessingContext> _executor;
    private readonly ILogger<CommandProcessingBehaviorTree>? _logger;
    private readonly double _lowConfidenceThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandProcessingBehaviorTree"/> class.
    /// </summary>
    /// <param name="lowConfidenceThreshold">The confidence threshold below which a command is considered ambiguous (default: 0.6).</param>
    /// <param name="logger">Optional logger instance.</param>
    public CommandProcessingBehaviorTree(
        double lowConfidenceThreshold = 0.6,
        ILogger<CommandProcessingBehaviorTree>? logger = null)
    {
        _logger = logger;
        _lowConfidenceThreshold = lowConfidenceThreshold;

        // Create logger for behavior tree nodes
        ILogger<BehaviorTreeNode<CommandProcessingContext>>? behaviorTreeLogger = null;
        ILogger<BehaviorTreeExecutor<CommandProcessingContext>>? executorLogger = null;
        if (logger != null)
        {
            // Create loggers with appropriate types
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug));
            behaviorTreeLogger = loggerFactory.CreateLogger<BehaviorTreeNode<CommandProcessingContext>>();
            executorLogger = loggerFactory.CreateLogger<BehaviorTreeExecutor<CommandProcessingContext>>();
        }

        // Build behavior tree
        // Root: Selector (try strategies in order)
        var root = new SelectorNode<CommandProcessingContext>("CommandProcessingSelector", behaviorTreeLogger)
            // Strategy 1: Simple Command (single intent, high confidence, complete)
            .AddChild(new SequenceNode<CommandProcessingContext>("SimpleCommandSequence", behaviorTreeLogger)
                .AddChild(new ConditionNode<CommandProcessingContext>(
                    "IsSimpleCommand",
                    ctx => IsSimpleCommand(ctx),
                    behaviorTreeLogger))
                .AddChild(new ActionNode<CommandProcessingContext>(
                    "ExecuteSimpleCommand",
                    async (ctx, ct) =>
                    {
                        ctx.Strategy = CommandProcessingStrategy.Simple;
                        ctx.ShouldExecuteDirectly = true;
                        _logger?.LogInformation(
                            "Command {CommandId} classified as Simple - executing directly",
                            ctx.CommandState.CommandId);
                        return BehaviorTreeNodeStatus.Success;
                    },
                    behaviorTreeLogger)))
            // Strategy 2: Multi-Step Command (multiple intents or complex workflow)
            .AddChild(new SequenceNode<CommandProcessingContext>("MultiStepCommandSequence", behaviorTreeLogger)
                .AddChild(new ConditionNode<CommandProcessingContext>(
                    "IsMultiStepCommand",
                    ctx => IsMultiStepCommand(ctx),
                    behaviorTreeLogger))
                .AddChild(new ActionNode<CommandProcessingContext>(
                    "ExecuteMultiStepCommand",
                    async (ctx, ct) =>
                    {
                        ctx.Strategy = CommandProcessingStrategy.MultiStep;
                        ctx.RequiresWorkflow = true;
                        _logger?.LogInformation(
                            "Command {CommandId} classified as MultiStep - executing workflow",
                            ctx.CommandState.CommandId);
                        return BehaviorTreeNodeStatus.Success;
                    },
                    behaviorTreeLogger)))
            // Strategy 3: Ambiguous Command (low confidence or incomplete)
            .AddChild(new SequenceNode<CommandProcessingContext>("AmbiguousCommandSequence", behaviorTreeLogger)
                .AddChild(new ConditionNode<CommandProcessingContext>(
                    "IsAmbiguousCommand",
                    ctx => IsAmbiguousCommand(ctx),
                    behaviorTreeLogger))
                .AddChild(new ActionNode<CommandProcessingContext>(
                    "RequestClarification",
                    async (ctx, ct) =>
                    {
                        ctx.Strategy = CommandProcessingStrategy.Ambiguous;
                        ctx.NeedsClarification = true;
                        ctx.ClarificationMessage = BuildClarificationMessage(ctx);
                        _logger?.LogInformation(
                            "Command {CommandId} classified as Ambiguous - requesting clarification",
                            ctx.CommandState.CommandId);
                        return BehaviorTreeNodeStatus.Success;
                    },
                    behaviorTreeLogger)));

        _tree = new BehaviorTree<CommandProcessingContext>("CommandProcessingTree", root);
        _executor = new BehaviorTreeExecutor<CommandProcessingContext>(executorLogger);
    }

    /// <summary>
    /// Processes a command using the behavior tree to determine the appropriate strategy.
    /// </summary>
    /// <param name="commandState">The command state to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processing context with the determined strategy.</returns>
    public async Task<CommandProcessingContext> ProcessCommandAsync(
        CommandState commandState,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandState);

        var context = new CommandProcessingContext
        {
            CommandState = commandState,
            Intent = commandState.Intent
        };

        var result = await _executor.ExecuteAsync(_tree, context, cancellationToken).ConfigureAwait(false);

        if (result.Status == BehaviorTreeNodeStatus.Success)
        {
            _logger?.LogDebug(
                "Command {CommandId} processed with strategy: {Strategy}",
                commandState.CommandId,
                context.Strategy);
        }
        else
        {
            _logger?.LogWarning(
                "Command {CommandId} processing failed - defaulting to MultiStep workflow",
                commandState.CommandId);
            // Default to multi-step workflow if behavior tree fails
            context.Strategy = CommandProcessingStrategy.MultiStep;
            context.RequiresWorkflow = true;
        }

        return context;
    }

    /// <summary>
    /// Checks if a command is a simple command (single intent, high confidence, complete).
    /// </summary>
    private bool IsSimpleCommand(CommandProcessingContext context)
    {
        if (context.Intent == null)
        {
            return false;
        }

        // Simple command criteria:
        // 1. High confidence
        // 2. Complete (no missing parameters)
        // 3. Single, clear intent
        return context.Intent.Confidence >= _lowConfidenceThreshold &&
               context.Intent.IsComplete &&
               !string.IsNullOrEmpty(context.Intent.Domain) &&
               !string.IsNullOrEmpty(context.Intent.Action);
    }

    /// <summary>
    /// Checks if a command is a multi-step command (requires workflow execution).
    /// </summary>
    private bool IsMultiStepCommand(CommandProcessingContext context)
    {
        if (context.Intent == null)
        {
            return false;
        }

        // Multi-step command criteria:
        // 1. Has intent but may have missing parameters
        // 2. Confidence is acceptable but not simple
        // 3. May require multiple steps or confirmations
        return context.Intent.Confidence >= _lowConfidenceThreshold &&
               (!context.Intent.IsComplete || context.CommandState.Status == CommandStatus.AwaitingConfirmation);
    }

    /// <summary>
    /// Checks if a command is ambiguous (low confidence or unclear).
    /// </summary>
    private bool IsAmbiguousCommand(CommandProcessingContext context)
    {
        if (context.Intent == null)
        {
            return true; // No intent = ambiguous
        }

        // Ambiguous command criteria:
        // 1. Low confidence
        // 2. Missing critical information
        // 3. Unclear intent
        return context.Intent.Confidence < _lowConfidenceThreshold ||
               (context.Intent.MissingRequired.Count > 0 && context.Intent.Confidence < 0.7) ||
               string.IsNullOrEmpty(context.Intent.Domain) ||
               string.IsNullOrEmpty(context.Intent.Action);
    }

    /// <summary>
    /// Builds a clarification message for ambiguous commands.
    /// </summary>
    private string BuildClarificationMessage(CommandProcessingContext context)
    {
        if (context.Intent == null)
        {
            return "I didn't understand that command. Could you please rephrase?";
        }

        var messages = new List<string>();

        if (context.Intent.Confidence < _lowConfidenceThreshold)
        {
            messages.Add("I'm not entirely sure what you meant.");
        }

        if (context.Intent.MissingRequired.Count > 0)
        {
            var missingParams = string.Join(", ", context.Intent.MissingRequired);
            messages.Add($"I need more information: {missingParams}");
        }

        if (string.IsNullOrEmpty(context.Intent.Domain) || string.IsNullOrEmpty(context.Intent.Action))
        {
            messages.Add("Could you clarify what you'd like me to do?");
        }

        return messages.Count > 0
            ? string.Join(" ", messages)
            : "Could you please provide more details?";
    }
}
