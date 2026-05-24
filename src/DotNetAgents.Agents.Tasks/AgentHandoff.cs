// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Standardized handoff structure for inter-agent communication.
/// Prevents information loss when passing work between agents in a pipeline.
/// Used by EducationAgent, PublishingAgent, and other DNA agent projects.
/// </summary>
/// <remarks>
/// Follows the AI-Writing-Assistant handoff standard: Context, Intention, Constraints, References.
/// The orchestrator packages context into AgentHandoff when invoking an agent;
/// the agent returns structured output; orchestrator passes to next agent via new handoff.
/// </remarks>
public record AgentHandoff
{
    /// <summary>
    /// Prior work summary — what the receiving agent needs to know about prior pipeline stages.
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>
    /// What the sending agent expects the receiver to accomplish.
    /// </summary>
    public string Intention { get; init; } = string.Empty;

    /// <summary>
    /// Boundaries the receiver must respect (domain, token budget, timeout, etc.).
    /// </summary>
    public HandoffConstraints Constraints { get; init; } = new();

    /// <summary>
    /// Content unit IDs, thread IDs, manuscript IDs, or other reference identifiers.
    /// </summary>
    public List<Guid> References { get; init; } = new();

    /// <summary>
    /// Structured data payload (manuscript, report, prior agent output, etc.).
    /// </summary>
    public object? Payload { get; init; }
}
