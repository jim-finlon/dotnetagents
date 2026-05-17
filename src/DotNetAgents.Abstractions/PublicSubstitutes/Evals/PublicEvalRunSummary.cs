namespace DotNetAgents.Abstractions.PublicSubstitutes.Evals;

/// <summary>Flattened public summary for a completed eval run.</summary>
public sealed record PublicEvalRunSummary(
    PublicEvalRunHandle Handle,
    string SuiteName,
    int TotalCount,
    int PassedCount,
    int FailedCount,
    double? AverageScore,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);
