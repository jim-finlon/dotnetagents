// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Chains;

/// <summary>
/// Stable chain execution envelope for OpenTelemetry correlation across runnable boundaries.
/// </summary>
public sealed record ChainTraceEnvelope
{
    public required string ChainId { get; init; }
    public string? RunId { get; init; }
    public string? AgentId { get; init; }
    public string? StepId { get; init; }
    public string? PromptRef { get; init; }
    public string? ToolName { get; init; }
    public string? CorrelationId { get; init; }
    public string? StoryId { get; init; }
}
