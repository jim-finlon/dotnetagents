// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// A workflow node that executes multiple child nodes in parallel.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public class ParallelNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IReadOnlyList<GraphNode<TState>> _childNodes;
    private readonly ParallelExecutionMode _executionMode;
    private readonly int? _requiredCount;
    private readonly ILogger<ParallelNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the parallel node.</param>
    /// <param name="childNodes">The child nodes to execute in parallel.</param>
    /// <param name="executionMode">The execution mode determining when to proceed. Default is All.</param>
    /// <param name="requiredCount">Required count for Count mode. Ignored for other modes.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when childNodes is empty or requiredCount is invalid.</exception>
    public ParallelNode(
        string name,
        IReadOnlyList<GraphNode<TState>> childNodes,
        ParallelExecutionMode executionMode = ParallelExecutionMode.All,
        int? requiredCount = null,
        ILogger<ParallelNode<TState>>? logger = null)
        : base(name, CreateHandler(
            childNodes ?? throw new ArgumentNullException(nameof(childNodes)),
            childNodes.Count == 0 ? throw new ArgumentException("Child nodes list cannot be empty.", nameof(childNodes)) : childNodes,
            executionMode,
            requiredCount,
            ValidateRequiredCount(executionMode, requiredCount, childNodes.Count),
            logger,
            name))
    {
        _childNodes = childNodes;
        _executionMode = executionMode;
        _requiredCount = requiredCount;
        _logger = logger;
        Description = $"Executes {childNodes.Count} nodes in parallel (mode: {executionMode})";
    }

    private static int ValidateRequiredCount(ParallelExecutionMode mode, int? requiredCount, int totalNodes)
    {
        if (mode == ParallelExecutionMode.Count)
        {
            if (!requiredCount.HasValue || requiredCount.Value < 1 || requiredCount.Value > totalNodes)
            {
                throw new ArgumentException(
                    $"RequiredCount must be between 1 and {totalNodes} for Count mode.",
                    nameof(requiredCount));
            }
            return requiredCount.Value;
        }
        return 0;
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IReadOnlyList<GraphNode<TState>> childNodes,
        IReadOnlyList<GraphNode<TState>> validatedNodes,
        ParallelExecutionMode executionMode,
        int? requiredCount,
        int validatedRequiredCount,
        ILogger<ParallelNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogInformation(
                "Node {NodeName}: Starting parallel execution of {Count} child nodes. Mode: {Mode}",
                nodeName,
                validatedNodes.Count,
                executionMode);

            // Create tasks for all child nodes
            var tasks = validatedNodes.Select((node, index) =>
            {
                var taskState = CloneState(state); // Each task gets its own copy
                return ExecuteChildNodeAsync(node, taskState, index, nodeName, logger, ct);
            }).ToList();

            // Wait based on execution mode
            var completedStates = await WaitForCompletionAsync(
                tasks,
                executionMode,
                validatedRequiredCount,
                nodeName,
                logger,
                ct).ConfigureAwait(false);

            // Merge results back into state
            var finalState = MergeStates(state, completedStates, nodeName, logger);

            logger?.LogInformation(
                "Node {NodeName}: Parallel execution completed. {CompletedCount}/{TotalCount} nodes completed.",
                nodeName,
                completedStates.Count,
                validatedNodes.Count);

            return finalState;
        };
    }

    private static async Task<List<(int Index, TState State)>> WaitForCompletionAsync(
        List<Task<(int Index, TState State)>> tasks,
        ParallelExecutionMode mode,
        int requiredCount,
        string nodeName,
        ILogger<ParallelNode<TState>>? logger,
        CancellationToken ct)
    {
        var completed = new List<(int Index, TState State)>();
        var remainingTasks = tasks.ToList();

        switch (mode)
        {
            case ParallelExecutionMode.All:
                // Wait for all tasks
                var allResults = await Task.WhenAll(remainingTasks).ConfigureAwait(false);
                return allResults.ToList();

            case ParallelExecutionMode.Any:
                // Wait for first task to complete
                var firstTask = await Task.WhenAny(remainingTasks).ConfigureAwait(false);
                var firstResult = await firstTask.ConfigureAwait(false);
                logger?.LogDebug("Node {NodeName}: First task completed (index {Index}).", nodeName, firstResult.Index);
                return new List<(int, TState)> { firstResult };

            case ParallelExecutionMode.Majority:
                // Wait for majority (more than 50%)
                var majorityCount = (tasks.Count / 2) + 1;
                return await WaitForCountAsync(remainingTasks, majorityCount, nodeName, logger, ct).ConfigureAwait(false);

            case ParallelExecutionMode.Count:
                // Wait for specific count
                return await WaitForCountAsync(remainingTasks, requiredCount, nodeName, logger, ct).ConfigureAwait(false);

            default:
                throw new ArgumentException($"Unknown execution mode: {mode}", nameof(mode));
        }
    }

    private static async Task<List<(int Index, TState State)>> WaitForCountAsync(
        List<Task<(int Index, TState State)>> tasks,
        int requiredCount,
        string nodeName,
        ILogger<ParallelNode<TState>>? logger,
        CancellationToken ct)
    {
        var completed = new List<(int Index, TState State)>();
        var remainingTasks = tasks.ToList();

        while (completed.Count < requiredCount && remainingTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(remainingTasks).ConfigureAwait(false);
            var result = await completedTask.ConfigureAwait(false);
            completed.Add(result);
            remainingTasks.Remove(completedTask);

            logger?.LogDebug(
                "Node {NodeName}: Task {Index} completed. Progress: {Completed}/{Required}",
                nodeName,
                result.Index,
                completed.Count,
                requiredCount);
        }

        return completed;
    }

    private static async Task<(int Index, TState State)> ExecuteChildNodeAsync(
        GraphNode<TState> node,
        TState state,
        int index,
        string parentNodeName,
        ILogger<ParallelNode<TState>>? logger,
        CancellationToken ct)
    {
        try
        {
            logger?.LogDebug(
                "Node {ParentNodeName}: Executing child node {ChildNodeName} (index {Index})",
                parentNodeName,
                node.Name,
                index);

            var result = await node.ExecuteAsync(state, ct).ConfigureAwait(false);
            return (index, result);
        }
        catch (Exception ex)
        {
            logger?.LogError(
                ex,
                "Node {ParentNodeName}: Error executing child node {ChildNodeName} (index {Index})",
                parentNodeName,
                node.Name,
                index);
            throw;
        }
    }

    private static TState CloneState(TState state)
    {
        // For now, we'll use shallow copy via serialization
        // In a real implementation, you might want to use a more efficient cloning mechanism
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            return System.Text.Json.JsonSerializer.Deserialize<TState>(json) ?? state;
        }
        catch
        {
            // If serialization fails, return the original state
            // This means all parallel tasks will share the same state reference
            return state;
        }
    }

    private static TState MergeStates(
        TState originalState,
        List<(int Index, TState State)> completedStates,
        string nodeName,
        ILogger<ParallelNode<TState>>? logger)
    {
        // For now, we'll merge by taking the last completed state
        // In a more sophisticated implementation, you might want to merge specific properties
        if (completedStates.Count == 0)
        {
            logger?.LogWarning("Node {NodeName}: No states to merge.", nodeName);
            return originalState;
        }

        // Use the last completed state as the merged result
        // Users can override this behavior by implementing custom merge logic in their state class
        var mergedState = completedStates.Last().State;

        logger?.LogDebug(
            "Node {NodeName}: Merged {Count} completed states.",
            nodeName,
            completedStates.Count);

        return mergedState;
    }
}
