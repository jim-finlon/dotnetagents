namespace DotNetAgents.Agents.Registry;

/// <summary>
/// Represents an agent's current status.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Agent is available and ready to accept tasks.
    /// </summary>
    Available = 1,

    /// <summary>
    /// Agent is currently busy processing tasks.
    /// </summary>
    Busy = 2,

    /// <summary>
    /// Agent is unavailable (e.g., maintenance, shutdown).
    /// </summary>
    Unavailable = 3,

    /// <summary>
    /// Agent is in an error state.
    /// </summary>
    Error = 4
}
