using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// A workflow node that wraps a state machine, allowing workflows to interact with state machines.
/// </summary>
/// <typeparam name="TState">The type of the workflow state (must match state machine context type).</typeparam>
public class StateMachineWorkflowNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IStateMachine<TState> _stateMachine;
    private readonly string _targetState;
    private readonly ILogger<StateMachineWorkflowNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachineWorkflowNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the workflow node.</param>
    /// <param name="stateMachine">The state machine to interact with.</param>
    /// <param name="targetState">The target state to transition to.</param>
    /// <param name="logger">Optional logger instance.</param>
    public StateMachineWorkflowNode(
        string name,
        IStateMachine<TState> stateMachine,
        string targetState,
        ILogger<StateMachineWorkflowNode<TState>>? logger = null)
        : base(name, CreateHandler(stateMachine, targetState, logger, name))
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _targetState = targetState ?? throw new ArgumentNullException(nameof(targetState));
        _logger = logger;
        Description = $"Transitions state machine to '{targetState}'";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IStateMachine<TState> stateMachine,
        string targetState,
        ILogger<StateMachineWorkflowNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            logger?.LogInformation("Node {NodeName}: Transitioning state machine to '{TargetState}'", nodeName, targetState);

            try
            {
                await stateMachine.TransitionAsync(targetState, state, ct).ConfigureAwait(false);
                logger?.LogInformation("Node {NodeName}: Successfully transitioned state machine to '{TargetState}'", nodeName, targetState);
            }
            catch (InvalidOperationException ex)
            {
                logger?.LogError(ex, "Node {NodeName}: Failed to transition state machine to '{TargetState}'", nodeName, targetState);
                throw new DotNetAgents.Abstractions.Exceptions.AgentException(
                    $"State machine transition failed: {ex.Message}",
                    DotNetAgents.Abstractions.Exceptions.ErrorCategory.WorkflowError,
                    ex);
            }

            return state;
        };
    }
}
