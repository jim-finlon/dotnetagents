// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Contracts;

/// <summary>
/// Versioned contract for a true DNA Agent. This lifts the catalog-level cognition profile into a
/// reusable artifact that can also declare capabilities, instruction artifacts, telemetry, and
/// forbidden surfaces for runtime, evaluation, and evolution workflows.
/// </summary>
public sealed record AgentContract
{
    public const string CurrentSchemaVersion = "dna.agent.contract.v1";

    public required string SchemaVersion { get; init; }
    public required string AgentId { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();
    public required string CognitionProfileRef { get; init; }
    public IReadOnlyList<string> ChainContractRefs { get; init; } = Array.Empty<string>();
    public required AgentInstructionArtifacts InstructionArtifacts { get; init; }
    public required AgentModelPolicy ModelPolicy { get; init; }
    public required AgentToolPolicy ToolPolicy { get; init; }
    public required AgentMemoryPolicy MemoryPolicy { get; init; }
    public required AgentContractTelemetryPolicy Telemetry { get; init; }
    public IReadOnlyList<string> ForbiddenSurfaces { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EvaluationSuites { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Versioned contract for a reasoning chain/workchain used by an Agent.
/// </summary>
public sealed record ChainContract
{
    public const string CurrentSchemaVersion = "dna.chain.contract.v1";

    public required string SchemaVersion { get; init; }
    public required string ChainId { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string Intent { get; init; }
    public required string EntryPointStepId { get; init; }
    public IReadOnlyList<ChainContractStep> Steps { get; init; } = Array.Empty<ChainContractStep>();
    public IReadOnlyList<string> PromptArtifactRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SkillRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ToolRefs { get; init; } = Array.Empty<string>();
    public string? InputSchemaRef { get; init; }
    public string? OutputSchemaRef { get; init; }
    public required AgentMemoryPolicy MemoryPolicy { get; init; }
    public required AgentContractTelemetryPolicy Telemetry { get; init; }
    public IReadOnlyList<string> ForbiddenSurfaces { get; init; } = Array.Empty<string>();
}

public sealed record AgentInstructionArtifacts
{
    public required string SystemPromptRef { get; init; }
    public IReadOnlyList<string> PromptArtifactRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SkillRefs { get; init; } = Array.Empty<string>();
}

public sealed record AgentContractTelemetryPolicy
{
    public IReadOnlyList<string> RequiredSpanAttributes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredMetricRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredOutcomeRefs { get; init; } = Array.Empty<string>();
}

public sealed record ChainContractStep
{
    public required string StepId { get; init; }
    public required string DisplayName { get; init; }
    public required string Kind { get; init; }
    public IReadOnlyList<string> UsesPromptRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> UsesToolRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EmitsOutcomeRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> NextStepIds { get; init; } = Array.Empty<string>();
}
