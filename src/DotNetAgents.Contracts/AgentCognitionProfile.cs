namespace DotNetAgents.Contracts;

/// <summary>
/// Declares the LLM-backed reasoning contract for a true DNA Agent.
/// Deterministic tools may be attached to this profile, but they are not Agents by themselves.
/// </summary>
public sealed record AgentCognitionProfile
{
    public const string CurrentSchemaVersion = "dna.agent.cognition.profile.v1";

    public required string SchemaVersion { get; init; }
    public required string ProfileId { get; init; }
    public required bool LlmBacked { get; init; }
    public required string ReasoningRole { get; init; }
    public required AgentReasoningProfile ReasoningProfile { get; init; }
    public required AgentModelPolicy ModelPolicy { get; init; }
    public required AgentPromptChainContract PromptChain { get; init; }
    public required AgentToolPolicy ToolPolicy { get; init; }
    public required AgentMemoryPolicy MemoryPolicy { get; init; }
    public required AgentTelemetryPolicy Telemetry { get; init; }
    public required AgentEvaluationSandboxPolicy EvaluationSandbox { get; init; }
}

public sealed record AgentReasoningProfile
{
    public required string DefaultCognitionTier { get; init; }
    public IReadOnlyList<string> AllowedCognitionTiers { get; init; } = Array.Empty<string>();
    public required string MinimumReasoningEffort { get; init; }
    public required string EscalationPolicy { get; init; }
}

public sealed record AgentModelPolicy
{
    public required string RouterRef { get; init; }
    public required string DefaultModelRef { get; init; }
    public IReadOnlyList<string> FallbackModelRefs { get; init; } = Array.Empty<string>();
    public required string CostPolicy { get; init; }
}

public sealed record AgentPromptChainContract
{
    public required string SystemPromptRef { get; init; }
    public required string ChainContractRef { get; init; }
    public IReadOnlyList<string> SkillRefs { get; init; } = Array.Empty<string>();
}

public sealed record AgentToolPolicy
{
    public IReadOnlyList<string> DeterministicToolRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiresPreviewFor { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ForbiddenToolRefs { get; init; } = Array.Empty<string>();
}

public sealed record AgentMemoryPolicy
{
    public IReadOnlyList<string> ReadableScopes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WritableScopes { get; init; } = Array.Empty<string>();
    public required string ProjectionPolicy { get; init; }
}

public sealed record AgentTelemetryPolicy
{
    public IReadOnlyList<string> RequiredSpanAttributes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredOutcomeRefs { get; init; } = Array.Empty<string>();
}

public sealed record AgentEvaluationSandboxPolicy
{
    public IReadOnlyList<string> EvaluationSuites { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> VariantableSurfaces { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FitnessSignals { get; init; } = Array.Empty<string>();
}
