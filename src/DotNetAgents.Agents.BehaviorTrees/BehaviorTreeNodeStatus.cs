namespace DotNetAgents.Agents.BehaviorTrees;

/// <summary>
/// Represents the execution status of a behavior tree node.
/// </summary>
public enum BehaviorTreeNodeStatus
{
    /// <summary>
    /// The node executed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The node execution failed.
    /// </summary>
    Failure,

    /// <summary>
    /// The node is currently running (for nodes that take multiple ticks).
    /// </summary>
    Running
}
