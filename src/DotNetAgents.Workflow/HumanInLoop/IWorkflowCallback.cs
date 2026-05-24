// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Workflow.HumanInLoop;

/// <summary>
/// Interface for workflow execution callbacks.
/// </summary>
/// <typeparam name="TState">The type of the workflow state.</typeparam>
public interface IWorkflowCallback<TState> where TState : class
{
    /// <summary>
    /// Called before a node is executed.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node about to be executed.</param>
    /// <param name="state">The current workflow state.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous callback.</returns>
    Task OnNodeExecutingAsync(
        string workflowRunId,
        string nodeName,
        TState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a node is executed.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="nodeName">The name of the node that was executed.</param>
    /// <param name="previousState">The state before node execution.</param>
    /// <param name="newState">The state after node execution.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous callback.</returns>
    Task OnNodeExecutedAsync(
        string workflowRunId,
        string nodeName,
        TState previousState,
        TState newState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a workflow execution completes.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="finalState">The final workflow state.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous callback.</returns>
    Task OnWorkflowCompletedAsync(
        string workflowRunId,
        TState finalState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when a workflow execution fails.
    /// </summary>
    /// <param name="workflowRunId">The ID of the workflow run.</param>
    /// <param name="state">The workflow state at the time of failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous callback.</returns>
    Task OnWorkflowFailedAsync(
        string workflowRunId,
        TState state,
        Exception exception,
        CancellationToken cancellationToken = default);
}
