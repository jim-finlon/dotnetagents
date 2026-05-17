namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Defines how parallel node execution should wait for completion.
/// </summary>
public enum ParallelExecutionMode
{
    /// <summary>
    /// Wait for all nodes to complete before proceeding.
    /// </summary>
    All,

    /// <summary>
    /// Proceed as soon as any node completes (first to finish).
    /// </summary>
    Any,

    /// <summary>
    /// Proceed when a majority of nodes complete (more than 50%).
    /// </summary>
    Majority,

    /// <summary>
    /// Proceed when a specific count of nodes complete.
    /// </summary>
    Count
}
