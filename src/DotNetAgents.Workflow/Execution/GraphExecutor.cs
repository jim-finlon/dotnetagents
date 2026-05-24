// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Abstractions.Execution;
using DotNetAgents.Workflow.Checkpoints;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Execution;

/// <summary>
/// Options for graph execution.
/// </summary>
public record GraphExecutionOptions
{
    /// <summary>
    /// Gets or sets the maximum number of iterations allowed.
    /// </summary>
    public int MaxIterations { get; init; } = 100;

    /// <summary>
    /// Gets or sets whether to validate the graph before execution.
    /// </summary>
    public bool ValidateGraph { get; init; } = true;

    /// <summary>
    /// Gets or sets an optional execution context for tracking.
    /// </summary>
    public DotNetAgents.Abstractions.Execution.ExecutionContext? ExecutionContext { get; init; }

    /// <summary>
    /// Gets or sets whether to create checkpoints after each node execution.
    /// </summary>
    public bool EnableCheckpointing { get; init; } = false;

    /// <summary>
    /// Gets or sets the checkpoint interval (number of nodes between checkpoints).
    /// Only used when <see cref="EnableCheckpointing"/> is true.
    /// </summary>
    public int CheckpointInterval { get; init; } = 1;
}

/// <summary>
/// Executes state graphs (workflows).
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class GraphExecutor<TState> where TState : class
{
    private readonly StateGraph<TState> _graph;
    private readonly ILogger<GraphExecutor<TState>>? _logger;
    private readonly ICheckpointStore<TState>? _checkpointStore;
    private readonly IStateSerializer<TState>? _stateSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphExecutor{TState}"/> class.
    /// </summary>
    /// <param name="graph">The state graph to execute.</param>
    /// <param name="logger">Optional logger for execution tracking.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/> is null.</exception>
    public GraphExecutor(StateGraph<TState> graph, ILogger<GraphExecutor<TState>>? logger = null)
        : this(graph, null, null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphExecutor{TState}"/> class with checkpoint support.
    /// </summary>
    /// <param name="graph">The state graph to execute.</param>
    /// <param name="checkpointStore">Optional checkpoint store for state persistence.</param>
    /// <param name="stateSerializer">Optional state serializer. If null and checkpointStore is provided, JsonStateSerializer is used.</param>
    /// <param name="logger">Optional logger for execution tracking.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/> is null.</exception>
    public GraphExecutor(
        StateGraph<TState> graph,
        ICheckpointStore<TState>? checkpointStore,
        IStateSerializer<TState>? stateSerializer = null,
        ILogger<GraphExecutor<TState>>? logger = null)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _checkpointStore = checkpointStore;
        _stateSerializer = stateSerializer ?? (checkpointStore != null ? new JsonStateSerializer<TState>() : null);
        _logger = logger;
    }

    /// <summary>
    /// Executes the graph starting from the entry point with the given initial state.
    /// </summary>
    /// <param name="initialState">The initial state.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The final state after execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialState"/> is null.</exception>
    /// <exception cref="AgentException">Thrown when execution fails or exceeds max iterations.</exception>
    public async Task<TState> ExecuteAsync(
        TState initialState,
        GraphExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (initialState == null)
            throw new ArgumentNullException(nameof(initialState));

        options ??= new GraphExecutionOptions();

        if (options.ValidateGraph)
        {
            _graph.Validate();
        }

        if (_graph.EntryPoint == null)
        {
            throw new AgentException(
                "Graph has no entry point.",
                ErrorCategory.WorkflowError);
        }

        var runId = GetRunIdFromState(initialState) ?? Guid.NewGuid().ToString("N");
        var currentState = initialState;
        var currentNode = _graph.EntryPoint;
        var iterationCount = 0;
        var executionPath = new List<string> { currentNode };

        _logger?.LogInformation(
            "Starting graph execution. Entry point: {EntryPoint}, Run ID: {RunId}",
            currentNode,
            runId);

        while (currentNode != null && iterationCount < options.MaxIterations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if we've reached an exit point
            if (_graph.ExitPoints.Contains(currentNode))
            {
                _logger?.LogInformation(
                    "Reached exit point: {ExitPoint}. Total iterations: {Iterations}",
                    currentNode,
                    iterationCount);
                break;
            }

            // Execute the current node
            if (!_graph.Nodes.TryGetValue(currentNode, out var node))
            {
                throw new AgentException(
                    $"Node '{currentNode}' not found in graph.",
                    ErrorCategory.WorkflowError);
            }

            _logger?.LogDebug(
                "Executing node: {NodeName}. Iteration: {Iteration}",
                currentNode,
                iterationCount + 1);

            try
            {
                currentState = await node.ExecuteAsync(currentState, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error executing node '{NodeName}' at iteration {Iteration}",
                    currentNode,
                    iterationCount + 1);
                throw new AgentException(
                    $"Error executing node '{currentNode}': {ex.Message}",
                    ErrorCategory.WorkflowError,
                    ex);
            }

            // Create checkpoint if enabled
            if (options.EnableCheckpointing && _checkpointStore != null && _stateSerializer != null)
            {
                if (iterationCount % options.CheckpointInterval == 0)
                {
                    await CreateCheckpointAsync(
                        runId,
                        currentNode,
                        currentState,
                        options,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            // Find the next node(s) by evaluating outgoing edges
            var outgoingEdges = _graph.GetOutgoingEdges(currentNode, currentState);

            if (outgoingEdges.Count == 0)
            {
                // No outgoing edges - check if this is an exit point
                if (!_graph.ExitPoints.Contains(currentNode))
                {
                    throw new AgentException(
                        $"Node '{currentNode}' has no outgoing edges and is not an exit point.",
                        ErrorCategory.WorkflowError);
                }
                break;
            }

            if (outgoingEdges.Count > 1)
            {
                // Multiple edges - take the first one that matches
                // In the future, we could support parallel execution or explicit selection
                _logger?.LogWarning(
                    "Multiple edges available from node '{NodeName}'. Taking first matching edge.",
                    currentNode);
            }

            var nextEdge = outgoingEdges[0];
            currentNode = nextEdge.To;
            executionPath.Add(currentNode);
            iterationCount++;

            _logger?.LogDebug(
                "Transitioning from '{FromNode}' to '{ToNode}'",
                nextEdge.From,
                nextEdge.To);
        }

        if (iterationCount >= options.MaxIterations)
        {
            throw new AgentException(
                $"Graph execution exceeded maximum iterations ({options.MaxIterations}). Execution path: {string.Join(" -> ", executionPath)}",
                ErrorCategory.WorkflowError);
        }

        _logger?.LogInformation(
            "Graph execution completed. Total iterations: {Iterations}. Final node: {FinalNode}",
            iterationCount,
            currentNode);

        return currentState;
    }

    /// <summary>
    /// Resumes graph execution from a checkpoint.
    /// </summary>
    /// <param name="checkpointId">The ID of the checkpoint to resume from.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The final state after execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="checkpointId"/> is null or whitespace.</exception>
    /// <exception cref="AgentException">Thrown when checkpoint is not found or execution fails.</exception>
    public async Task<TState> ResumeAsync(
        string checkpointId,
        GraphExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(checkpointId))
            throw new ArgumentException("Checkpoint ID cannot be null or whitespace.", nameof(checkpointId));

        if (_checkpointStore == null || _stateSerializer == null)
        {
            throw new AgentException(
                "Cannot resume execution: checkpoint store and serializer are required.",
                ErrorCategory.WorkflowError);
        }

        var checkpoint = await _checkpointStore.GetAsync(checkpointId, cancellationToken).ConfigureAwait(false);
        if (checkpoint == null)
        {
            throw new AgentException(
                $"Checkpoint '{checkpointId}' not found.",
                ErrorCategory.WorkflowError);
        }

        options ??= new GraphExecutionOptions();
        if (!options.EnableCheckpointing)
        {
            options = options with { EnableCheckpointing = true };
        }

        _logger?.LogInformation(
            "Resuming execution from checkpoint '{CheckpointId}' at node '{NodeName}'",
            checkpointId,
            checkpoint.NodeName);

        var state = _stateSerializer.Deserialize(checkpoint.SerializedState);
        var currentNode = checkpoint.NodeName;

        // Continue execution from the checkpoint node
        return await ExecuteFromNodeAsync(
            checkpoint.RunId,
            state,
            currentNode,
            options,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resumes graph execution from the latest checkpoint for a workflow run.
    /// </summary>
    /// <param name="runId">The workflow run identifier.</param>
    /// <param name="options">Optional execution options.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The final state after execution.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="runId"/> is null or whitespace.</exception>
    /// <exception cref="AgentException">Thrown when no checkpoint is found or execution fails.</exception>
    public async Task<TState> ResumeFromLatestAsync(
        string runId,
        GraphExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run ID cannot be null or whitespace.", nameof(runId));

        if (_checkpointStore == null || _stateSerializer == null)
        {
            throw new AgentException(
                "Cannot resume execution: checkpoint store and serializer are required.",
                ErrorCategory.WorkflowError);
        }

        var checkpoint = await _checkpointStore.GetLatestAsync(runId, cancellationToken).ConfigureAwait(false);
        if (checkpoint == null)
        {
            throw new AgentException(
                $"No checkpoint found for run '{runId}'.",
                ErrorCategory.WorkflowError);
        }

        return await ResumeAsync(checkpoint.Id, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TState> ExecuteFromNodeAsync(
        string runId,
        TState initialState,
        string startNode,
        GraphExecutionOptions options,
        CancellationToken cancellationToken)
    {
        if (options.ValidateGraph)
        {
            _graph.Validate();
        }

        if (!_graph.Nodes.ContainsKey(startNode))
        {
            throw new AgentException(
                $"Start node '{startNode}' not found in graph.",
                ErrorCategory.WorkflowError);
        }

        var currentState = initialState;
        var currentNode = startNode;
        var iterationCount = 0;
        var executionPath = new List<string> { currentNode };

        _logger?.LogInformation(
            "Resuming graph execution from node '{StartNode}'. Run ID: {RunId}",
            startNode,
            runId);

        while (currentNode != null && iterationCount < options.MaxIterations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if we've reached an exit point
            if (_graph.ExitPoints.Contains(currentNode))
            {
                _logger?.LogInformation(
                    "Reached exit point: {ExitPoint}. Total iterations: {Iterations}",
                    currentNode,
                    iterationCount);
                break;
            }

            // Execute the current node
            if (!_graph.Nodes.TryGetValue(currentNode, out var node))
            {
                throw new AgentException(
                    $"Node '{currentNode}' not found in graph.",
                    ErrorCategory.WorkflowError);
            }

            _logger?.LogDebug(
                "Executing node: {NodeName}. Iteration: {Iteration}",
                currentNode,
                iterationCount + 1);

            try
            {
                currentState = await node.ExecuteAsync(currentState, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error executing node '{NodeName}' at iteration {Iteration}",
                    currentNode,
                    iterationCount + 1);
                throw new AgentException(
                    $"Error executing node '{currentNode}': {ex.Message}",
                    ErrorCategory.WorkflowError,
                    ex);
            }

            // Create checkpoint if enabled
            if (options.EnableCheckpointing && _checkpointStore != null && _stateSerializer != null)
            {
                if (iterationCount % options.CheckpointInterval == 0)
                {
                    await CreateCheckpointAsync(
                        runId,
                        currentNode,
                        currentState,
                        options,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            // Find the next node(s) by evaluating outgoing edges
            var outgoingEdges = _graph.GetOutgoingEdges(currentNode, currentState);

            if (outgoingEdges.Count == 0)
            {
                // No outgoing edges - check if this is an exit point
                if (!_graph.ExitPoints.Contains(currentNode))
                {
                    throw new AgentException(
                        $"Node '{currentNode}' has no outgoing edges and is not an exit point.",
                        ErrorCategory.WorkflowError);
                }
                break;
            }

            if (outgoingEdges.Count > 1)
            {
                _logger?.LogWarning(
                    "Multiple edges available from node '{NodeName}'. Taking first matching edge.",
                    currentNode);
            }

            var nextEdge = outgoingEdges[0];
            currentNode = nextEdge.To;
            executionPath.Add(currentNode);
            iterationCount++;

            _logger?.LogDebug(
                "Transitioning from '{FromNode}' to '{ToNode}'",
                nextEdge.From,
                nextEdge.To);
        }

        if (iterationCount >= options.MaxIterations)
        {
            throw new AgentException(
                $"Graph execution exceeded maximum iterations ({options.MaxIterations}). Execution path: {string.Join(" -> ", executionPath)}",
                ErrorCategory.WorkflowError);
        }

        _logger?.LogInformation(
            "Graph execution completed. Total iterations: {Iterations}. Final node: {FinalNode}",
            iterationCount,
            currentNode);

        return currentState;
    }

    private static string? GetRunIdFromState(TState state)
    {
        if (state == null) return null;
        var type = typeof(TState);
        var prop = type.GetProperty("WorkflowRunId") ?? type.GetProperty("RunId");
        var value = prop?.GetValue(state)?.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private async Task CreateCheckpointAsync(
        string runId,
        string nodeName,
        TState state,
        GraphExecutionOptions options,
        CancellationToken cancellationToken)
    {
        if (_checkpointStore == null || _stateSerializer == null)
            return;

        try
        {
            var checkpointId = Guid.NewGuid().ToString("N");
            var serializedState = _stateSerializer.Serialize(state);
            var metadata = new Dictionary<string, object>
            {
                ["iteration"] = 0, // Will be updated if we track iteration
                ["executionContext"] = options.ExecutionContext?.CorrelationId ?? string.Empty
            };

            var checkpoint = new Checkpoint<TState>
            {
                Id = checkpointId,
                RunId = runId,
                NodeName = nodeName,
                SerializedState = serializedState,
                CreatedAt = DateTime.UtcNow,
                StateVersion = 1,
                Metadata = metadata
            };

            await _checkpointStore.SaveAsync(checkpoint, cancellationToken).ConfigureAwait(false);

            _logger?.LogDebug(
                "Created checkpoint '{CheckpointId}' at node '{NodeName}' for run '{RunId}'",
                checkpointId,
                nodeName,
                runId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "Failed to create checkpoint at node '{NodeName}'",
                nodeName);
            // Don't throw - checkpointing failures shouldn't stop execution
        }
    }
}
