// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Interface for handling human approval requests in workflows.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IApprovalHandler<TState> where TState : class
{
    /// <summary>
    /// Requests approval for a workflow state transition.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting approval.</param>
    /// <param name="state">The current workflow state.</param>
    /// <param name="message">Optional message describing what needs approval.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if approved; false if rejected.</returns>
    Task<bool> RequestApprovalAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        string? message = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if approval has been granted for a workflow state transition.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node requesting approval.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if approved; false if not approved or still pending.</returns>
    Task<bool> IsApprovedAsync(
        string workflowRunId,
        string nodeName,
        CancellationToken cancellationToken = default);
}
