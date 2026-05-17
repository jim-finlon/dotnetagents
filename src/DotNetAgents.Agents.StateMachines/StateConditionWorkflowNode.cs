using DotNetAgents.Workflow.Graph;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.StateMachines;

/// <summary>
/// A workflow node that checks if a state machine is in a specific state.
/// Used for conditional branching in workflows based on state machine state.
/// </summary>
/// <typeparam name="TState">The type of the workflow state (must match state machine context type).</typeparam>
public class StateConditionWorkflowNode<TState> : GraphNode<TState> where TState : class
{
    private readonly IStateMachine<TState> _stateMachine;
    private readonly string _requiredState;
    private readonly ILogger<StateConditionWorkflowNode<TState>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateConditionWorkflowNode{TState}"/> class.
    /// </summary>
    /// <param name="name">The name of the workflow node.</param>
    /// <param name="stateMachine">The state machine to check.</param>
    /// <param name="requiredState">The required state for the condition to pass.</param>
    /// <param name="logger">Optional logger instance.</param>
    public StateConditionWorkflowNode(
        string name,
        IStateMachine<TState> stateMachine,
        string requiredState,
        ILogger<StateConditionWorkflowNode<TState>>? logger = null)
        : base(name, CreateHandler(stateMachine, requiredState, logger, name))
    {
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _requiredState = requiredState ?? throw new ArgumentNullException(nameof(requiredState));
        _logger = logger;
        Description = $"Checks if state machine is in '{requiredState}' state";
    }

    private static Func<TState, CancellationToken, Task<TState>> CreateHandler(
        IStateMachine<TState> stateMachine,
        string requiredState,
        ILogger<StateConditionWorkflowNode<TState>>? logger,
        string nodeName)
    {
        return async (state, ct) =>
        {
            ArgumentNullException.ThrowIfNull(state);
            ct.ThrowIfCancellationRequested();

            var currentState = stateMachine.CurrentState;
            var isInRequiredState = currentState.Equals(requiredState, StringComparison.OrdinalIgnoreCase);

            logger?.LogInformation(
                "Node {NodeName}: Checking state machine state. Current: '{CurrentState}', Required: '{RequiredState}', Match: {Match}",
                nodeName, currentState, requiredState, isInRequiredState);

            if (!isInRequiredState)
            {
                throw new DotNetAgents.Abstractions.Exceptions.AgentException(
                    $"State machine is in '{currentState}' state, but '{requiredState}' is required.",
                    DotNetAgents.Abstractions.Exceptions.ErrorCategory.WorkflowError);
            }

            return state;
        };
    }
}
