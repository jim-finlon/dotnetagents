using DotNetAgents.Agents.StateMachines;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// A behavior tree node that checks if a state machine is in a specific state.
/// </summary>
/// <typeparam name="TContext">The type of the execution context.</typeparam>
public class StateMachineConditionNode<TContext> : BehaviorTreeNode<TContext> where TContext : class
{
    private readonly IStateMachine<TContext> _stateMachine;
    private readonly string _requiredState;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineConditionNode{TContext}"/> class.
    /// </summary>
    /// <param name="name">The name of the node.</param>
    /// <param name="stateMachine">The state machine to check.</param>
    /// <param name="requiredState">The required state for the condition to pass.</param>
    /// <param name="logger">Optional logger instance.</param>
    public StateMachineConditionNode(
        string name,
        IStateMachine<TContext> stateMachine,
        string requiredState,
        ILogger<BehaviorTreeNode<TContext>>? logger = null)
        : base(name, logger)
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _requiredState = requiredState ?? throw new ArgumentNullException(nameof(requiredState));
    }

    /// <inheritdoc/>
    public override Task<BehaviorTreeNodeStatus> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Logger?.LogDebug("Evaluating state machine condition node '{NodeName}' for state '{RequiredState}'", Name, _requiredState);

        var currentState = _stateMachine.CurrentState;
        var isInRequiredState = currentState.Equals(_requiredState, StringComparison.OrdinalIgnoreCase);

        Logger?.LogDebug("State machine condition node '{NodeName}': Current='{CurrentState}', Required='{RequiredState}', Match={Match}",
            Name, currentState, _requiredState, isInRequiredState);

        var status = isInRequiredState ? BehaviorTreeNodeStatus.Success : BehaviorTreeNodeStatus.Failure;
        return Task.FromResult(status);
    }
}
