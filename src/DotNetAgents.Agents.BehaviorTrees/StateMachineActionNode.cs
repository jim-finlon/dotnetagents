using DotNetAgents.Agents.StateMachines;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A behavior tree node that triggers a state machine transition.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class StateMachineActionNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    private readonly IStateMachine<TContext> _stateMachine;
    private readonly string _targetState;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineActionNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="stateMachine">The state machine to transition.</param>
    /// <param name="targetState">The target state to transition to.</param>
    /// <param name="logger">Optional logger instance.</param>
    public StateMachineActionNode(
        string name,
        IStateMachine<TContext> stateMachine,
        string targetState,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _targetState = targetState ?? throw new ArgumentNullException(nameof(targetState));
    }

    /// <inheritdoc/>
    public override async Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Logger?.LogDebug("Executing state machine action node '{NodeName}' to transition to '{TargetState}'", Name, _targetState);

        try
        {
            await _stateMachine.TransitionAsync(_targetState, context, cancellationToken).ConfigureAwait(false);
            Logger?.LogDebug("State machine action node '{NodeName}' completed successfully", Name);
            return BehaviorTreeNodeStatus.Success;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "State machine action node '{NodeName}' failed to transition to '{TargetState}'", Name, _targetState);
            return BehaviorTreeNodeStatus.Failure;
        }
    }
}
