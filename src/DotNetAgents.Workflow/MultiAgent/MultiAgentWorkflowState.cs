using DotNetAgents.Agents.Tasks;

namespace DotNetAgents.Workflow.MultiAgent;

/// <summary>
/// Base state class for multi-agent workflows.
/// Extend this class to add your own workflow-specific state properties.
/// </summary>
public class MultiAgentWorkflowState
{
    /// <summary>
    /// Gets or sets the list of tasks that have been submitted to workers.
    /// </summary>
    public List<WorkerTask> SubmittedTasks { get; set; } = new();

    /// <summary>
    /// Gets or sets the dictionary of task results, keyed by task ID.
    /// </summary>
    public Dictionary<string, WorkerTaskResult> TaskResults { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of task IDs that are currently pending.
    /// </summary>
    public List<string> PendingTaskIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of task IDs that have completed successfully.
    /// </summary>
    public List<string> CompletedTaskIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of task IDs that have failed.
    /// </summary>
    public List<string> FailedTaskIds { get; set; } = new();

    /// <summary>
    /// Gets or sets additional workflow metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
