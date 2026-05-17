using DotNetAgents.Abstractions.Exceptions;
using DotNetAgents.Workflow.Execution;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// A workflow node that executes a sub-workflow (nested workflow) as part of the main workflow.
/// </summary>
/// <typeparam name="TState">The type of the main workflow state.</typeparam>
/// <typeparam name="TSubState">The type of the sub-workflow state.</typeparam>
public class SubWorkflowNode<TState, TSubState> : GraphNode<TState>
    where TState : class
    where TSubState : class
{
    private readonly StateGraph<TSubState> _subWorkflow;
    private readonly Func<TState, TSubState> _stateMapper;
    private readonly Func<TSubState, TState, TState> _resultMapper;
    private readonly ILogger<SubWorkflowNode<TState, TSubState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubWorkflowNode{TState, TSubState}"/> class.
    /// </summary>
    /// <param name="name">The name of the sub-workflow node.</param>
    /// <param name="subWorkflow">The sub-workflow graph to execute.</param>
    /// <param name="stateMapper">A function that maps the main workflow state to the sub-workflow state.</param>
    /// <param name="resultMapper">A function that maps the sub-workflow result back to the main workflow state.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public SubWorkflowNode(
        string name,
        StateGraph<TSubState> subWorkflow,
        Func<TState, TSubState> stateMapper,
        Func<TSubState, TState, TState> resultMapper,
        ILogger<SubWorkflowNode<TState, TSubState>>? logger = null)
        : base(name, CreateHandler(
            subWorkflow ?? throw new ArgumentNullException(nameof(subWorkflow)),
            stateMapper ?? throw new ArgumentNullException(nameof(stateMapper)),
            resultMapper ?? throw new ArgumentNullException(nameof(resultMapper)),
            logger,
            name))
    {
        _subWorkflow = subWorkflow;
        _stateMapper = stateMapper;
        _resultMapper = resultMapper;
        _logger = logger;
        Description = $"Executes sub-workflow '{subWorkflow.EntryPoint}'";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        StateGraph<TSubState> subWorkflow,
        Func<TState, TSubState> stateMapper,
        Func<TSubState, TState, TState> resultMapper,
        ILogger<SubWorkflowNode<TState, TSubState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogInformation(
                "Node {NodeName}: Starting sub-workflow execution. Entry point: {EntryPoint}",
                nodeName,
                subWorkflow.EntryPoint ?? "unknown");

            // Map main state to sub-workflow state
            TSubState subState;
            try
            {
                subState = stateMapper(state);
                if (subState == null)
                {
                    throw new AgentException(
                        $"State mapper returned null for node '{nodeName}'.",
                        ErrorCategory.WorkflowError);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node {NodeName}: Error mapping state to sub-workflow state.", nodeName);
                throw new AgentException(
                    $"State mapping failed for node '{nodeName}': {ex.Message}",
                    ErrorCategory.WorkflowError,
                    ex);
            }

            // Execute sub-workflow
            TSubState subResult;
            try
            {
                var executor = new GraphExecutor<TSubState>(subWorkflow, null); // Logger not needed for sub-workflow
                subResult = await executor.ExecuteAsync(subState, cancellationToken: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node {NodeName}: Error executing sub-workflow.", nodeName);
                throw new AgentException(
                    $"Sub-workflow execution failed in node '{nodeName}': {ex.Message}",
                    ErrorCategory.WorkflowError,
                    ex);
            }

            // Map sub-workflow result back to main state
            TState finalState;
            try
            {
                finalState = resultMapper(subResult, state);
                if (finalState == null)
                {
                    throw new AgentException(
                        $"Result mapper returned null for node '{nodeName}'.",
                        ErrorCategory.WorkflowError);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node {NodeName}: Error mapping sub-workflow result back to main state.", nodeName);
                throw new AgentException(
                    $"Result mapping failed for node '{nodeName}': {ex.Message}",
                    ErrorCategory.WorkflowError,
                    ex);
            }

            logger?.LogInformation("Node {NodeName}: Sub-workflow execution completed successfully.", nodeName);

            return finalState;
        };
    }
}
