namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Constraints applied when handing off work between agents.
/// Used with <see cref="AgentHandoff"/> for inter-agent communication.
/// </summary>
public record HandoffConstraints
{
    /// <summary>
    /// Optional domain scope (e.g., KnowledgeDomain for EducationAgent).
    /// </summary>
    public Guid? DomainId { get; init; }

    /// <summary>
    /// Optional level scope (e.g., ContentLevel for EducationAgent).
    /// </summary>
    public Guid? LevelId { get; init; }

    /// <summary>
    /// Maximum token budget for the receiving agent's context.
    /// </summary>
    public int? MaxTokenBudget { get; init; }

    /// <summary>
    /// Maximum iterations (e.g. ReAct loop) for the receiving agent.
    /// </summary>
    public int? MaxIterations { get; init; }

    /// <summary>
    /// Timeout for the receiving agent's execution.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Whether the receiving agent must apply safety filtering to output.
    /// Default is true for content delivered to users.
    /// </summary>
    public bool RequiresSafetyFilter { get; init; } = true;
}
