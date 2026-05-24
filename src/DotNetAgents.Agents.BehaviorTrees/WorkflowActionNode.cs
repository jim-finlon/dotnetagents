// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Workflow.Execution;
using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A behavior tree node that executes a workflow as an action.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class WorkflowActionNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    private readonly StateGraph<TContext> _workflow;
    private readonly Func<TContext, TContext>? _contextMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowActionNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="workflow">The workflow to execute.</param>
    /// <param name="contextMapper">Optional function to map context before workflow execution.</param>
    /// <param name="logger">Optional logger instance.</param>
    public WorkflowActionNode(
        string name,
        StateGraph<TContext> workflow,
        Func<TContext, TContext>? contextMapper = null,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _contextMapper = contextMapper;
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Logger?.LogDebug("Executing workflow action node '{NodeName}'", Name);

        try
        {
            var workflowContext = _contextMapper != null ? _contextMapper(context) : context;
            var executor = new GraphExecutor<TContext>(_workflow);
            var result = await executor.ExecuteAsync(workflowContext, options: null, cancellationToken).ConfigureAwait(false);

            Logger?.LogDebug("Workflow action node '{NodeName}' completed successfully", Name);
            return BehaviorTreeNodeStatus.Success;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Workflow action node '{NodeName}' failed", Name);
            return BehaviorTreeNodeStatus.Failure;
        }
    }
}
