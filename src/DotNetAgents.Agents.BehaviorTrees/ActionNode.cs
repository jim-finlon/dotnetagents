using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A leaf node that executes an action.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class ActionNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    private readonly Func<TContext, CancellationToken, Task<BehaviorTreeNodeStatus>> _action;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ActionNode(
        string name,
        Func<TContext, CancellationToken, Task<BehaviorTreeNodeStatus>> action,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionNode{TContext}"/> class with a synchronous action.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="action">The synchronous action to execute.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ActionNode(
        string name,
        Func<TContext, BehaviorTreeNodeStatus> action,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        ArgumentNullException.ThrowIfNull(action);
        _action = (context, ct) => Task.FromResult(action(context));
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Logger?.LogDebug("Executing action node '{NodeName}'", Name);

        try
        {
            var status = await _action(context, cancellationToken).ConfigureAwait(false);
            Logger?.LogDebug("Action node '{NodeName}' completed with status '{Status}'", Name, status);
            return status;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Action node '{NodeName}' failed", Name);
            return BehaviorTreeNodeStatus.Failure;
        }
    }
}
