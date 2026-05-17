using DotNetAgents.Workflow.Graph;
using DotNetAgents.Workflow.HumanInLoop;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Patterns;

/// <summary>
/// Provides common workflow patterns that can be used as building blocks.
/// </summary>
public static class WorkflowPatterns
{
    /// <summary>
    /// Creates an approval chain pattern where multiple approvals are required sequentially.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="approvers">List of approver names or identifiers.</param>
    /// <param name="approvalHandler">The approval handler to use.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A workflow graph representing the approval chain.</returns>
    public static StateGraph<TState> CreateApprovalChain<TState>(
        IReadOnlyList<string> approvers,
        IApprovalHandler<TState> approvalHandler,
        ILogger? logger = null)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(approvers);
        ArgumentNullException.ThrowIfNull(approvalHandler);

        if (approvers.Count == 0)
        {
            throw new ArgumentException("At least one approver is required.", nameof(approvers));
        }

        var graph = new StateGraph<TState>();
        var previousNode = (string?)null;

        for (int i = 0; i < approvers.Count; i++)
        {
            var approver = approvers[i];
            var nodeName = $"approval-{i + 1}-{approver}";
            var approvalNode = new ApprovalNode<TState>(
                nodeName,
                approvalHandler,
                $"Approval required from {approver}");

            graph.AddNode(approvalNode);

            if (previousNode != null)
            {
                graph.AddEdge(previousNode, nodeName);
            }
            else
            {
                graph.SetEntryPoint(nodeName);
            }

            previousNode = nodeName;
        }

        if (previousNode != null)
        {
            graph.AddExitPoint(previousNode);
        }

        return graph;
    }

    /// <summary>
    /// Creates a parallel processing pattern where multiple nodes execute in parallel.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="processors">List of processor nodes to execute in parallel.</param>
    /// <param name="executionMode">The parallel execution mode. Default is All.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A workflow graph representing parallel processing.</returns>
    public static StateGraph<TState> CreateParallelProcessing<TState>(
        IReadOnlyList<GraphNode<TState>> processors,
        Graph.ParallelExecutionMode executionMode = Graph.ParallelExecutionMode.All,
        ILogger? logger = null)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(processors);

        if (processors.Count == 0)
        {
            throw new ArgumentException("At least one processor is required.", nameof(processors));
        }

        var graph = new StateGraph<TState>();
        var parallelNode = new ParallelNode<TState>(
            "parallel-process",
            processors,
            executionMode,
            logger: logger as ILogger<ParallelNode<TState>>);

        graph.AddNode(parallelNode);
        graph.SetEntryPoint("parallel-process");
        graph.AddExitPoint("parallel-process");

        return graph;
    }

    /// <summary>
    /// Creates a retry loop pattern that retries a node on failure.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="nodeToRetry">The node to retry on failure.</param>
    /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
    /// <param name="initialDelay">Initial delay before first retry. Default is 1 second.</param>
    /// <param name="backoffMultiplier">Exponential backoff multiplier. Default is 2.0.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A workflow graph with retry logic.</returns>
    public static StateGraph<TState> CreateRetryLoop<TState>(
        GraphNode<TState> nodeToRetry,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0,
        ILogger? logger = null)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(nodeToRetry);

        var graph = new StateGraph<TState>();
        var retryNode = new RetryNode<TState>(
            "retry-node",
            nodeToRetry,
            maxRetries,
            initialDelay,
            backoffMultiplier,
            logger: logger as ILogger<RetryNode<TState>>);

        graph.AddNode(retryNode);
        graph.SetEntryPoint("retry-node");
        graph.AddExitPoint("retry-node");

        return graph;
    }

    /// <summary>
    /// Creates a conditional workflow pattern with validation and branching.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="validator">The validation node.</param>
    /// <param name="onValidNode">The node to execute when validation passes.</param>
    /// <param name="onInvalidNode">The node to execute when validation fails. If null, workflow ends.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A workflow graph with conditional branching based on validation.</returns>
    public static StateGraph<TState> CreateConditionalWorkflow<TState>(
        ValidationNode<TState> validator,
        GraphNode<TState> onValidNode,
        GraphNode<TState>? onInvalidNode = null,
        ILogger? logger = null)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(onValidNode);

        var graph = new StateGraph<TState>();

        graph.AddNode(validator);
        graph.AddNode(onValidNode);
        if (onInvalidNode != null)
        {
            graph.AddNode(onInvalidNode);
        }

        graph.SetEntryPoint(validator.Name);

        // Add conditional edge for valid path
        graph.AddEdge(
            validator.Name,
            onValidNode.Name,
            ValidationNode<TState>.CreateValidationPassedCondition());

        // Add conditional edge for invalid path
        if (onInvalidNode != null)
        {
            graph.AddEdge(
                validator.Name,
                onInvalidNode.Name,
                state => !ValidationNode<TState>.CreateValidationPassedCondition()(state));
        }
        else
        {
            graph.AddExitPoint(validator.Name);
        }

        graph.AddExitPoint(onValidNode.Name);
        if (onInvalidNode != null)
        {
            graph.AddExitPoint(onInvalidNode.Name);
        }

        return graph;
    }

    /// <summary>
    /// Creates a sequential workflow pattern with validation at each step.
    /// </summary>
    /// <typeparam name="TState">The type of the workflow state.</typeparam>
    /// <param name="steps">List of nodes to execute sequentially, each with optional validation.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <returns>A workflow graph with sequential execution and validation.</returns>
    public static StateGraph<TState> CreateSequentialWithValidation<TState>(
        IReadOnlyList<(GraphNode<TState> Node, ValidationNode<TState>? Validator)> steps,
        ILogger? logger = null)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (steps.Count == 0)
        {
            throw new ArgumentException("At least one step is required.", nameof(steps));
        }

        var graph = new StateGraph<TState>();
        string? previousNode = null;

        for (int i = 0; i < steps.Count; i++)
        {
            var (node, validator) = steps[i];
            graph.AddNode(node);

            if (validator != null)
            {
                graph.AddNode(validator);
                graph.AddEdge(previousNode ?? validator.Name, validator.Name);
                graph.AddEdge(validator.Name, node.Name, ValidationNode<TState>.CreateValidationPassedCondition());
                previousNode = node.Name;
            }
            else
            {
                if (previousNode != null)
                {
                    graph.AddEdge(previousNode, node.Name);
                }
                else
                {
                    graph.SetEntryPoint(node.Name);
                }
                previousNode = node.Name;
            }
        }

        if (previousNode != null)
        {
            graph.AddExitPoint(previousNode);
        }

        return graph;
    }
}
