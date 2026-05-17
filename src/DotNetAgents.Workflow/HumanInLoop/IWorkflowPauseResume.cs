namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Interface for pausing and resuming workflow execution.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IWorkflowPauseResume<TState> where TState : class
{
    /// <summary>
    /// Pauses a workflow execution.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run to pause.</param>
    /// <param name="reason">Optional reason for pausing.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the workflow was paused; false if it was not found or already paused.</returns>
    Task<bool> PauseAsync(
        string workflowRunId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused workflow execution.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run to resume.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the workflow was resumed; false if it was not found or not paused.</returns>
    Task<bool> ResumeAsync(
        string workflowRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a workflow is paused.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the workflow is paused; otherwise, false.</returns>
    Task<bool> IsPausedAsync(
        string workflowRunId,
        CancellationToken cancellationToken = default);
}
