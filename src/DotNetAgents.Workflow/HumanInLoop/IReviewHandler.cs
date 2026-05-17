namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Interface for handling human review requests in workflows.
/// Review requests allow humans to review and potentially modify workflow state before proceeding.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IReviewHandler<TState> where TState : class
{
    /// <summary>
    /// Requests a human to review the workflow state.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the review.</param>
    /// <param name="state">The current workflow state to review.</param>
    /// <param name="context">Optional context describing what needs to be reviewed.</param>
    /// <param name="allowModification">Whether the human is allowed to modify the state.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The reviewed (and potentially modified) state, or null if review is pending.</returns>
    Task<TState?> RequestReviewAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        string? context = null,
        bool allowModification = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a review has been completed for a workflow state transition.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting the review.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The reviewed state if review is complete; otherwise, null.</returns>
    Task<TState?> GetReviewedStateAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default);
}
