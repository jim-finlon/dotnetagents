// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Workflow;

namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Supervisor agent that delegates tasks to worker agents.
/// Story c4b3b3e5 — the Workflow-side <see cref="IWorkerDelegationSink"/> seam
/// owns the Submit/GetStatus/GetResult/Cancel surface so Workflow no longer
/// has to depend on this project. <see cref="ISupervisorAgent"/> extends that
/// seam and contributes only the Supervisor-specific extras (statistics).
/// Any consumer that previously passed an <see cref="ISupervisorAgent"/> into
/// a Workflow node keeps working — the reference is implicitly upcast to
/// <see cref="IWorkerDelegationSink"/>.
/// </summary>
public interface ISupervisorAgent : IWorkerDelegationSink
{
    /// <summary>
    /// Gets statistics about task execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Statistics about supervisor task execution.</returns>
    Task<SupervisorStatistics> GetStatisticsAsync(
        CancellationToken cancellationToken = default);
}
