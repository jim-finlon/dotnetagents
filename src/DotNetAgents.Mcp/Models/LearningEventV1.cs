// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Canonical DNA learning event payload used by agents and orchestrators.
/// Matches the dna.learning.event.v1 envelope defined in DNA docs.
/// </summary>
public record LearningEventV1
{
    public string EventType { get; init; } = LearningEventTypes.DnaLearningEventV1;
    public string EventId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;

    public string SourceService { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Project { get; init; } = string.Empty;

    public LearningEventActor Actor { get; init; } = new();
    public string ActorType { get; init; } = "agent";
    public string ActorId { get; init; } = string.Empty;

    public string? TaskFamily { get; init; }
    public string WorkflowId { get; init; } = string.Empty;
    public string Step { get; init; } = string.Empty;
    public string Intent { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;

    public string? InputSummary { get; init; }
    public double Confidence { get; init; }
    public IReadOnlyDictionary<string, double> Scores { get; init; } = new Dictionary<string, double>(StringComparer.Ordinal);
    public LearningEventCost? Cost { get; init; }
    public long DurationMs { get; init; }
    public long TimeCostMs { get; init; }
    public string ProblemSignature { get; init; } = string.Empty;
    public string LessonSummary { get; init; } = string.Empty;
    public string? InputsHash { get; init; }

    public IReadOnlyList<string> ArtifactRefs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();

    public IReadOnlyList<LearningEventError> Errors { get; init; } = Array.Empty<LearningEventError>();
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? FixApplied { get; init; }

    public string PrivacyClass { get; init; } = LearningEventPrivacyClasses.Internal;
    public string RetentionClass { get; init; } = LearningEventRetentionClasses.Standard;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

/// <summary>
/// Preferred type name for new DNA code. Kept separate from <see cref="LearningEventV1"/> so existing agents continue to compile.
/// </summary>
public sealed record DnaLearningEvent : LearningEventV1;

public sealed record LearningEventActor
{
    public string Type { get; init; } = "agent";
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
}

public sealed record LearningEventCost
{
    public string? Provider { get; init; }
    public string? Model { get; init; }
    public long? InputTokens { get; init; }
    public long? OutputTokens { get; init; }
    public decimal? TotalUsd { get; init; }
}

public sealed record LearningEventError
{
    public string? Code { get; init; }
    public string? Message { get; init; }
    public bool? IsTransient { get; init; }
}

public static class LearningEventTypes
{
    public const string DnaLearningEventV1 = "dna.learning.event.v1";
}

public static class LearningEventPrivacyClasses
{
    public const string Public = "public";
    public const string Internal = "internal";
    public const string Confidential = "confidential";
    public const string Restricted = "restricted";
}

public static class LearningEventRetentionClasses
{
    public const string Ephemeral = "ephemeral";
    public const string Standard = "standard";
    public const string Audit = "audit";
}
