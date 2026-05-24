// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Agents.Tasks;
using TaskStatus = DotNetAgents.Agents.Tasks.TaskStatus;

namespace DotNetAgents.Workflow;

/// <summary>
/// Story c4b3b3e5 — dependency-inversion seam between
/// <see cref="MultiAgent.DelegateToWorkerNode{TState}"/> /
/// <see cref="MultiAgent.AggregateResultsNode{TState}"/> /
/// <see cref="Graph.QualityGateNode{TState}"/> and any worker-orchestration
/// provider (the canonical implementation is
/// <c>DotNetAgents.Agents.Supervisor.ISupervisorAgent</c>, which extends this
/// interface for free).
///
/// Before c4b3b3e5 these workflow nodes took <c>ISupervisorAgent</c> directly,
/// which forced <c>DotNetAgents.Workflow</c> to depend on
/// <c>DotNetAgents.Agents.Supervisor</c>. That direction is backwards: Workflow
/// is the abstraction layer, Supervisor is one concrete worker-orchestration
/// provider. Owning the contract in Workflow lets the dependency arrow flip
/// (Supervisor → Workflow) and lets other providers (or test doubles)
/// implement worker delegation without dragging in the Supervisor runtime.
/// </summary>
public interface IWorkerDelegationSink
{
    /// <summary>
    /// Submits a task to be executed by a worker agent.
    /// </summary>
    /// <param name="task">The task to submit.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the submitted task.</returns>
    Task<string> SubmitTaskAsync(
        WorkerTask task,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits multiple tasks for parallel execution.
    /// </summary>
    /// <param name="tasks">The tasks to submit.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The IDs of the submitted tasks.</returns>
    Task<IReadOnlyList<string>> SubmitTasksAsync(
        IEnumerable<WorkerTask> tasks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a task.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The status of the task.</returns>
    Task<TaskStatus> GetTaskStatusAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the result of a completed task.
    /// </summary>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The task result, or null if the task is not completed.</returns>
    Task<WorkerTaskResult?> GetTaskResultAsync(
        string taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a pending or running task.
    /// </summary>
    /// <param name="taskId">The ID of the task to cancel.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the task was cancelled, false if it was not found or already completed.</returns>
    Task<bool> CancelTaskAsync(
        string taskId,
        CancellationToken cancellationToken = default);
}
