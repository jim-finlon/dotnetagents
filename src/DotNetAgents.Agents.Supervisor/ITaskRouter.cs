using DotNetAgents.Agents.Tasks;
using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.Supervisor;

/// <summary>
/// Interface for task routing strategies.
/// This allows SupervisorAgent to use different routing strategies without direct dependencies.
/// </summary>
public interface ITaskRouter
{
    /// <summary>
    /// Routes a task to an appropriate worker.
    /// </summary>
    /// <param name="task">The task to route.</param>
    /// <param name="availableWorkers">The list of available workers.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The selected worker, or null if no suitable worker found.</returns>
    Task<AgentInfo?> RouteTaskAsync(
        WorkerTask task,
        IReadOnlyList<AgentInfo> availableWorkers,
        CancellationToken cancellationToken = default);
}
