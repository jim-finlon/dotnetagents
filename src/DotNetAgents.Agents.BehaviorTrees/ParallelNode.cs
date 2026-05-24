// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A composite node that executes all children in parallel.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class ParallelNode<TContext> : CompositeNode<TContext> where TContext : class
{
    private readonly ParallelPolicy _policy;

    /// <summary>
    /// Defines the policy for parallel execution.
    /// </summary>
    public enum ParallelPolicy
    {
        /// <summary>
        /// Succeeds if all children succeed, fails if any child fails.
        /// </summary>
        RequireAll,

        /// <summary>
        /// Succeeds if any child succeeds, fails if all children fail.
        /// </summary>
        RequireOne
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="policy">The parallel execution policy.</param>
    /// <param name="logger">Optional logger instance.</param>
    public ParallelNode(
        string name,
        ParallelPolicy policy = ParallelPolicy.RequireAll,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _policy = policy;
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (Children.Count == 0)
        {
            Logger?.LogWarning("Parallel node '{NodeName}' has no children", Name);
            return BehaviorTreeNodeStatus.Success;
        }

        Logger?.LogDebug("Executing parallel node '{NodeName}' with {ChildCount} children (policy: {Policy})",
            Name, Children.Count, _policy);

        var tasks = Children.Select(child => child.ExecuteAsync(context, cancellationToken)).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        var successCount = results.Count(r => r == BehaviorTreeNodeStatus.Success);
        var failureCount = results.Count(r => r == BehaviorTreeNodeStatus.Failure);
        var runningCount = results.Count(r => r == BehaviorTreeNodeStatus.Running);

        if (runningCount > 0)
        {
            Logger?.LogDebug("Parallel node '{NodeName}' is running ({RunningCount} children running)", Name, runningCount);
            return BehaviorTreeNodeStatus.Running;
        }

        BehaviorTreeNodeStatus status;
        if (_policy == ParallelPolicy.RequireAll)
        {
            status = failureCount == 0 ? BehaviorTreeNodeStatus.Success : BehaviorTreeNodeStatus.Failure;
        }
        else // RequireOne
        {
            status = successCount > 0 ? BehaviorTreeNodeStatus.Success : BehaviorTreeNodeStatus.Failure;
        }

        Logger?.LogDebug("Parallel node '{NodeName}' completed with status '{Status}' (Success: {SuccessCount}, Failure: {FailureCount})",
            Name, status, successCount, failureCount);
        return status;
    }
}
