using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Represents the result of an agent execution step.
/// </summary>
public record AgentStepResult
{
    /// <summary>
    /// Gets or sets the output from this step.
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the agent should continue executing.
    /// </summary>
    public bool ShouldContinue { get; init; } = true;

    /// <summary>
    /// Gets or sets the tool that was called, if any.
    /// </summary>
    public ITool? ToolCalled { get; init; }

    /// <summary>
    /// Gets or sets the result from the tool execution, if any.
    /// </summary>
    public ToolResult? ToolResult { get; init; }

    /// <summary>
    /// Gets or sets metadata about this step.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }
}
