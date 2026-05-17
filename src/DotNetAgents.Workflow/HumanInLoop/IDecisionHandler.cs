namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Interface for handling human decision requests in workflows.
/// Decisions allow humans to choose from multiple options, not just approve/reject.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IDecisionHandler<TState> where TState : class
{
    /// <summary>
    /// Requests a decision from a human, presenting multiple options.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the decision.</param>
    /// <param name="state">The current workflow state.</param>
    /// <param name="question">The question to present to the human.</param>
    /// <param name="options">The available options to choose from.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The selected option, or null if no decision has been made yet.</returns>
    Task<string?> RequestDecisionAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        string question,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a decision has been made for a workflow state transition.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the decision.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The selected option if a decision has been made; otherwise, null.</returns>
    Task<string?> GetDecisionAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default);
}
