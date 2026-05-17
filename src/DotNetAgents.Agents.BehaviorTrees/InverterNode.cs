using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A decorator node that inverts the result of its child.
/// Success becomes Failure, Failure becomes Success, Running remains Running.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class InverterNode<TContext> : DecoratorNode<TContext> where TContext : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InverterNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="logger">Optional logger instance.</param>
    public InverterNode(string name, ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Child == null)
        {
            Logger?.LogWarning("Inverter node '{NodeName}' has no child", Name);
            return BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Executing inverter node '{NodeName}'", Name);

        var status = await Child.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);

        var invertedStatus = status switch
        {
            BehaviorTreeNodeStatus.Success => BehaviorTreeNodeStatus.Failure,
            BehaviorTreeNodeStatus.Failure => BehaviorTreeNodeStatus.Success,
            _ => status // Running remains Running
        };

        Logger?.LogDebug("Inverter node '{NodeName}' inverted status from '{OriginalStatus}' to '{InvertedStatus}'",
            Name, status, invertedStatus);

        return invertedStatus;
    }
}
