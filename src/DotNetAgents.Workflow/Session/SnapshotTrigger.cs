namespace DotNetAgents.Workflow.Session;

/// <summary>
/// Represents what triggered a snapshot.
/// </summary>
public enum SnapshotTrigger
{
    /// <summary>
    /// Manual snapshot creation.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Milestone completion.
    /// </summary>
    MilestoneCompleted = 1,

    /// <summary>
    /// Scheduled snapshot.
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Error or failure recovery point.
    /// </summary>
    ErrorRecovery = 3,

    /// <summary>
    /// Workflow completion.
    /// </summary>
    WorkflowCompleted = 4,

    /// <summary>
    /// Workflow pause.
    /// </summary>
    WorkflowPaused = 5
}
