// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Intent;
using DotNetAgents.Mcp.Routing;
using DotNetAgents.Voice.BehaviorTrees;
using DotNetAgents.Voice.IntentClassification;
using DotNetAgents.Voice.Notifications;
using DotNetAgents.Voice.Parsing;
using DotNetAgents.Voice.StateMachines;
using DotNetAgents.Workflow.Execution;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Orchestration;

/// <summary>
/// Orchestrates voice command workflows using the DotNetAgents workflow engine.
/// </summary>
public class CommandWorkflowOrchestrator : ICommandWorkflowOrchestrator
{
    private readonly ICommandParser _parser;
    private readonly IMcpAdapterRouter _adapterRouter;
    private readonly ILogger<CommandWorkflowOrchestrator> _logger;
    private readonly ICommandNotificationService? _notificationService;
    private readonly StateGraph<CommandState> _workflowGraph;
    private readonly IVoiceSessionStateMachine<VoiceSessionContext>? _sessionStateMachine;
    private readonly Dictionary<Guid, VoiceSessionContext> _sessionContexts = new();
    private readonly CommandProcessingBehaviorTree? _behaviorTree;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandWorkflowOrchestrator"/> class.
    /// </summary>
    /// <param name="parser">The command parser.</param>
    /// <param name="adapterRouter">The MCP adapter router.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="notificationService">Optional notification service for real-time updates.</param>
    /// <param name="sessionStateMachine">Optional voice session state machine for tracking session lifecycle.</param>
    /// <param name="behaviorTree">Optional behavior tree for intelligent command processing strategy determination.</param>
    public CommandWorkflowOrchestrator(
        ICommandParser parser,
        IMcpAdapterRouter adapterRouter,
        ILogger<CommandWorkflowOrchestrator> logger,
        ICommandNotificationService? notificationService = null,
        IVoiceSessionStateMachine<VoiceSessionContext>? sessionStateMachine = null,
        CommandProcessingBehaviorTree? behaviorTree = null)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _adapterRouter = adapterRouter ?? throw new ArgumentNullException(nameof(adapterRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService;
        _sessionStateMachine = sessionStateMachine;
        _behaviorTree = behaviorTree;

        _workflowGraph = BuildWorkflowGraph();
    }

    /// <inheritdoc />
    public async Task<CommandState> ExecuteAsync(
        CommandState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        _logger.LogInformation(
            "Starting workflow execution for command {CommandId}",
            state.CommandId);

        // Get or create session context for state machine tracking
        VoiceSessionContext? sessionContext = null;
        if (_sessionStateMachine != null)
        {
            lock (_sessionContexts)
            {
                if (!_sessionContexts.TryGetValue(state.UserId, out sessionContext))
                {
                    sessionContext = new VoiceSessionContext
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        UserId = state.UserId,
                        CurrentCommandId = state.CommandId,
                        VoiceInput = state.RawText
                    };
                    _sessionContexts[state.UserId] = sessionContext;
                }
                else
                {
                    sessionContext.CurrentCommandId = state.CommandId;
                    sessionContext.VoiceInput = state.RawText;
                }
            }

            // Transition to Listening state (voice input detected)
            try
            {
                sessionContext.ListeningStartedAt = DateTimeOffset.UtcNow;
                await _sessionStateMachine.TransitionAsync("Listening", sessionContext, cancellationToken)
                    .ConfigureAwait(false);
                _logger.LogDebug("Voice session {SessionId} transitioned to Listening", sessionContext.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to transition voice session to Listening state");
            }
        }

        // Send initial status update (if notification service is available)
        if (_notificationService != null)
        {
            await SendStatusUpdateAsync(state, CommandStatus.Queued, "Command queued for processing", cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            // Transition to Processing state (input complete, starting processing)
            if (_sessionStateMachine != null && sessionContext != null)
            {
                try
                {
                    sessionContext.ProcessingStartedAt = DateTimeOffset.UtcNow;
                    await _sessionStateMachine.TransitionAsync("Processing", sessionContext, cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogDebug("Voice session {SessionId} transitioned to Processing", sessionContext.SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to transition voice session to Processing state");
                }
            }

            // Use behavior tree to determine processing strategy (if available)
            CommandProcessingStrategy strategy = CommandProcessingStrategy.MultiStep;
            if (_behaviorTree != null)
            {
                try
                {
                    var processingContext = await _behaviorTree.ProcessCommandAsync(state, cancellationToken)
                        .ConfigureAwait(false);
                    strategy = processingContext.Strategy;

                    // Handle ambiguous commands - request clarification
                    if (strategy == CommandProcessingStrategy.Ambiguous && processingContext.NeedsClarification)
                    {
                        if (_notificationService != null && !string.IsNullOrEmpty(processingContext.ClarificationMessage))
                        {
                            await SendClarificationRequestAsync(
                                state.UserId,
                                state.CommandId,
                                processingContext.ClarificationMessage,
                                "command",
                                cancellationToken).ConfigureAwait(false);
                        }

                        return state with
                        {
                            Status = CommandStatus.AwaitingClarification
                        };
                    }

                    // Log strategy decision
                    _logger.LogInformation(
                        "Command {CommandId} processing strategy: {Strategy}",
                        state.CommandId,
                        strategy);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Behavior tree processing failed, using default workflow");
                }
            }

            var executor = new GraphExecutor<CommandState>(_workflowGraph);
            var finalState = await executor.ExecuteAsync(state, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Command {CommandId} completed with status {Status}",
                finalState.CommandId,
                finalState.Status);

            // Transition to Responding state (response ready)
            if (_sessionStateMachine != null && sessionContext != null)
            {
                try
                {
                    sessionContext.RespondingStartedAt = DateTimeOffset.UtcNow;
                    await _sessionStateMachine.TransitionAsync("Responding", sessionContext, cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogDebug("Voice session {SessionId} transitioned to Responding", sessionContext.SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to transition voice session to Responding state");
                }
            }

            // Send final status update (if notification service is available)
            if (_notificationService != null)
            {
                if (finalState.Status == CommandStatus.Completed)
                {
                    await SendCompletionAsync(finalState, cancellationToken).ConfigureAwait(false);
                }
                else if (finalState.Status == CommandStatus.Failed)
                {
                    await SendErrorAsync(finalState, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await SendStatusUpdateAsync(finalState, finalState.Status, null, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            // Transition back to Idle state (response complete)
            if (_sessionStateMachine != null && sessionContext != null)
            {
                try
                {
                    await _sessionStateMachine.TransitionAsync("Idle", sessionContext, cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogDebug("Voice session {SessionId} transitioned to Idle", sessionContext.SessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to transition voice session to Idle state");
                }
            }

            return finalState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandId} failed during execution", state.CommandId);

            // Transition to Error state
            if (_sessionStateMachine != null && sessionContext != null)
            {
                try
                {
                    sessionContext.ErrorCount++;
                    sessionContext.LastErrorMessage = ex.Message;
                    await _sessionStateMachine.TransitionAsync("Error", sessionContext, cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogDebug("Voice session {SessionId} transitioned to Error", sessionContext.SessionId);

                    // Attempt recovery: Error → Idle
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                    await _sessionStateMachine.TransitionAsync("Idle", sessionContext, cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogDebug("Voice session {SessionId} recovered from Error to Idle", sessionContext.SessionId);
                }
                catch (Exception stateEx)
                {
                    _logger.LogWarning(stateEx, "Failed to transition voice session to Error/Idle state");
                }
            }

            return state with
            {
                Status = CommandStatus.Failed,
                Error = ex.Message,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    private StateGraph<CommandState> BuildWorkflowGraph()
    {
        var graph = new StateGraph<CommandState>();

        // Parse node
        graph.AddNode(new GraphNode<CommandState>(
            "parse",
            async (state, ct) =>
            {
                IntentContext? intentContext = string.IsNullOrWhiteSpace(state.UserMemoryContext)
                    ? null
                    : new IntentContext { LongTermUserMemory = state.UserMemoryContext };

                var intent = await _parser.ParseAsync(state.RawText, intentContext, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "Workflow parse: commandId={CommandId} intent={Intent} targetService={TargetService} tool={Tool} confidence={Confidence:F2}",
                    state.CommandId,
                    intent.FullName,
                    intent.TargetService,
                    intent.Tool,
                    intent.Confidence);

                return state with
                {
                    Status = CommandStatus.Parsing,
                    Intent = intent,
                    TargetService = intent.TargetService
                };
            }));

        // Check completeness node
        graph.AddNode(new GraphNode<CommandState>(
            "check_completeness",
            async (state, ct) =>
            {
                if (state.Intent?.IsComplete == false)
                {
                    _logger.LogInformation(
                        "Command {CommandId} requires clarification for parameters: {MissingParams}",
                        state.CommandId,
                        string.Join(", ", state.Intent.MissingRequired));

                    var updatedState = state with { Status = CommandStatus.AwaitingClarification };

                    if (_notificationService != null && state.Intent.MissingRequired.Any())
                    {
                        var missingParam = state.Intent.MissingRequired.First();
                        var prompt = $"Please provide {missingParam}";
                        await SendClarificationRequestAsync(
                            state.UserId,
                            state.CommandId,
                            prompt,
                            missingParam,
                            ct).ConfigureAwait(false);
                    }

                    return updatedState;
                }

                return state;
            }));

        // Confirm node
        graph.AddNode(new GraphNode<CommandState>(
            "confirm",
            async (state, ct) =>
            {
                if (!state.Confirmed)
                {
                    _logger.LogInformation(
                        "Command {CommandId} awaiting user confirmation",
                        state.CommandId);

                    var updatedState = state with
                    {
                        Status = CommandStatus.AwaitingConfirmation
                    };

                    if (_notificationService != null && state.Intent != null)
                    {
                        var readBackText = GenerateReadBackText(state);
                        await SendConfirmationRequestAsync(
                            state.UserId,
                            state.CommandId,
                            readBackText,
                            ct).ConfigureAwait(false);
                    }

                    return updatedState;
                }

                return state with
                {
                    Status = CommandStatus.Confirmed,
                    ConfirmedAt = DateTime.UtcNow
                };
            }));

        // Awaiting clarification terminal node.
        graph.AddNode(new GraphNode<CommandState>(
            "awaiting_clarification",
            (state, ct) => Task.FromResult(state)));

        // Awaiting confirmation terminal node.
        graph.AddNode(new GraphNode<CommandState>(
            "awaiting_confirmation",
            (state, ct) => Task.FromResult(state)));

        // Failed terminal node.
        graph.AddNode(new GraphNode<CommandState>(
            "failed",
            (state, ct) => Task.FromResult(state)));

        // Execute node
        graph.AddNode(new GraphNode<CommandState>(
            "execute",
            async (state, ct) =>
            {
                if (state.Intent == null)
                {
                    throw new InvalidOperationException("Cannot execute without intent");
                }

                _logger.LogInformation(
                    "Workflow execute: commandId={CommandId} intent={Intent} targetService={TargetService} tool={Tool}",
                    state.CommandId,
                    state.Intent.FullName,
                    state.Intent.TargetService,
                    state.Intent.Tool);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Copy parameters so KnowledgeMemory/MCP layers can add keys without mutating classified intent.
                var mcpParameters = new Dictionary<string, object>(state.Intent.Parameters);
                var taskKey = state.CommandId.ToString("N");
                mcpParameters["correlationId"] = taskKey;
                mcpParameters["taskId"] = taskKey;
                mcpParameters["orchestration.confirmed"] = state.Confirmed;
                if (!string.IsNullOrWhiteSpace(state.UserMemoryContext))
                {
                    mcpParameters[IntentParameterKeys.UserMemoryContext] = state.UserMemoryContext!;
                }

                try
                {
                    AgentDispatchIntent routed = state.Intent.WithParameters(mcpParameters);
                    var mcpIntent = new DotNetAgents.Mcp.Routing.Intent
                    {
                        Domain = routed.Domain,
                        Action = routed.Action,
                        SubType = routed.SubType,
                        Parameters = routed.Parameters,
                        TargetService = routed.TargetService,
                        Tool = routed.Tool,
                        MissingRequired = routed.MissingRequired,
                        Confidence = routed.Confidence,
                        RawText = routed.RawText
                    };

                    var result = await _adapterRouter.ExecuteIntentAsync(mcpIntent, ct)
                        .ConfigureAwait(false);
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "Workflow execute: commandId={CommandId} completed durationMs={DurationMs} resultType={ResultType} resultNull={ResultNull}",
                        state.CommandId,
                        (int)stopwatch.ElapsedMilliseconds,
                        result?.GetType().FullName ?? "(null)",
                        result is null);

                    var mcpCall = new McpCallResult
                    {
                        Service = state.TargetService ?? "unknown",
                        Tool = state.Intent.Tool ?? state.Intent.FullName,
                        Parameters = mcpParameters,
                        Result = result,
                        Success = true,
                        DurationMs = (int)stopwatch.ElapsedMilliseconds
                    };

                    return state with
                    {
                        Status = CommandStatus.Processing,
                        McpCalls = state.McpCalls.Append(mcpCall).ToList(),
                        Result = result
                    };
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        ex,
                        "Workflow execute: commandId={CommandId} MCP/router failed after {DurationMs}ms",
                        state.CommandId,
                        (int)stopwatch.ElapsedMilliseconds);

                    var mcpCall = new McpCallResult
                    {
                        Service = state.TargetService ?? "unknown",
                        Tool = state.Intent.Tool ?? state.Intent.FullName,
                        Parameters = mcpParameters,
                        Result = null,
                        Success = false,
                        DurationMs = (int)stopwatch.ElapsedMilliseconds,
                        Error = ex.Message
                    };

                    return state with
                    {
                        Status = CommandStatus.Failed,
                        McpCalls = state.McpCalls.Append(mcpCall).ToList(),
                        Error = ex.Message
                    };
                }
            }));

        // Complete node
        graph.AddNode(new GraphNode<CommandState>(
            "complete",
            async (state, ct) =>
            {
                return state with
                {
                    Status = CommandStatus.Completed,
                    CompletedAt = DateTime.UtcNow
                };
            }));

        // Build edges
        graph.AddEdge("parse", "check_completeness");
        graph.AddEdge("check_completeness", "confirm", state => state.Status != CommandStatus.AwaitingClarification);
        graph.AddEdge("check_completeness", "awaiting_clarification", state => state.Status == CommandStatus.AwaitingClarification);
        graph.AddEdge("confirm", "execute", state => state.Confirmed);
        graph.AddEdge("confirm", "awaiting_confirmation", state => !state.Confirmed);
        graph.AddEdge("execute", "complete", state => state.Status != CommandStatus.Failed);
        graph.AddEdge("execute", "failed", state => state.Status == CommandStatus.Failed);

        // Set entry and exit points
        graph.SetEntryPoint("parse");
        graph.AddExitPoint("complete");
        graph.AddExitPoint("awaiting_clarification");
        graph.AddExitPoint("awaiting_confirmation");
        graph.AddExitPoint("failed");

        return graph;
    }

    private Task SendStatusUpdateAsync(
        CommandState state,
        CommandStatus status,
        string? message,
        CancellationToken cancellationToken) =>
        _notificationService?.SendStatusUpdateAsync(
            state.UserId,
            state.CommandId,
            status,
            message,
            cancellationToken) ?? Task.CompletedTask;

    private Task SendClarificationRequestAsync(
        Guid userId,
        Guid commandId,
        string prompt,
        string missingParameter,
        CancellationToken cancellationToken) =>
        _notificationService?.SendClarificationRequestAsync(
            userId,
            commandId,
            prompt,
            missingParameter,
            turn: 1,
            maxTurns: 10,
            cancellationToken) ?? Task.CompletedTask;

    private Task SendConfirmationRequestAsync(
        Guid userId,
        Guid commandId,
        string readBackText,
        CancellationToken cancellationToken) =>
        _notificationService?.SendConfirmationRequestAsync(
            userId,
            commandId,
            readBackText,
            cancellationToken) ?? Task.CompletedTask;

    private Task SendCompletionAsync(
        CommandState state,
        CancellationToken cancellationToken) =>
        _notificationService?.SendCompletionAsync(
            state.UserId,
            state.CommandId,
            state.Result,
            cancellationToken) ?? Task.CompletedTask;

    private Task SendErrorAsync(
        CommandState state,
        CancellationToken cancellationToken) =>
        _notificationService?.SendErrorAsync(
            state.UserId,
            state.CommandId,
            state.Error ?? "Unknown error",
            cancellationToken) ?? Task.CompletedTask;

    private static string GenerateReadBackText(CommandState state)
    {
        if (state.Intent == null)
        {
            return state.RawText;
        }

        var parts = new List<string> { $"I understand you want to {state.Intent.Action}" };

        if (!string.IsNullOrEmpty(state.Intent.SubType))
        {
            parts.Add($"a {state.Intent.SubType}");
        }

        parts.Add($"in {state.Intent.Domain}");

        if (state.Intent.Parameters.Any())
        {
            var paramDescriptions = state.Intent.Parameters
                .Select(kvp => $"{kvp.Key}: {kvp.Value}")
                .ToList();
            parts.Add($"with {string.Join(", ", paramDescriptions)}");
        }

        return string.Join(" ", parts) + ". Is this correct?";
    }

    /// <inheritdoc />
    public VoiceSessionContext? GetSessionState(Guid userId)
    {
        lock (_sessionContexts)
        {
            return _sessionContexts.TryGetValue(userId, out var context) ? context : null;
        }
    }
}
