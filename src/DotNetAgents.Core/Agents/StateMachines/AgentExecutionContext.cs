namespace DotNetAgents.Core.Agents.StateMachines;

/// <summary>
/// Context object for agent execution state machine operations.
/// </summary>
public class AgentExecutionContext
{
    /// <summary>
    /// Gets or sets the execution identifier.
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the input to the agent.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Gets or sets the current iteration number.
    /// </summary>
    public int Iteration { get; set; }

    /// <summary>
    /// Gets or sets the maximum iterations allowed.
    /// </summary>
    public int MaxIterations { get; set; }

    /// <summary>
    /// Gets or sets when the execution was initialized.
    /// </summary>
    public DateTimeOffset? InitializedAt { get; set; }

    /// <summary>
    /// Gets or sets when thinking started.
    /// </summary>
    public DateTimeOffset? ThinkingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets when acting (tool execution) started.
    /// </summary>
    public DateTimeOffset? ActingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets when observing started.
    /// </summary>
    public DateTimeOffset? ObservingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets when finalizing started.
    /// </summary>
    public DateTimeOffset? FinalizingStartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the execution was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the selected tool name (if any).
    /// </summary>
    public string? SelectedToolName { get; set; }

    /// <summary>
    /// Gets or sets the number of tools executed.
    /// </summary>
    public int ToolsExecuted { get; set; }

    /// <summary>
    /// Gets or sets the number of errors encountered.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the execution is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether a final answer has been reached.
    /// </summary>
    public bool HasFinalAnswer { get; set; }
}
