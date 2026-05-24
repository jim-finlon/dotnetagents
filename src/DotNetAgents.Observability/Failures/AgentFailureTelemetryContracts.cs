// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Observability.Failures;

public enum AgentFailureKind
{
    Unknown,
    Exception,
    OddOutput,
    DependencyDegraded,
    Timeout,
    PolicyRefusal,
    FallbackInvoked
}

public enum AgentFailureSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum AgentFallbackDisposition
{
    None,
    Retry,
    UseAlternateProvider,
    DegradeGracefully,
    EscalateToOperator,
    OpenSdlcFollowUp
}

public sealed record AgentFailureEvent(
    string Id,
    DateTimeOffset OccurredAtUtc,
    string ActorId,
    string Operation,
    AgentFailureKind Kind,
    AgentFailureSeverity Severity,
    string Summary,
    string? Details = null,
    string? Dependency = null,
    string? TraceId = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string>? Tags = null);

public sealed record AgentFallbackEvent(
    string Id,
    DateTimeOffset OccurredAtUtc,
    string FailureEventId,
    AgentFallbackDisposition Disposition,
    string Action,
    bool Succeeded,
    string? Notes = null);

public sealed record AgentFallbackRule(
    AgentFailureKind Kind,
    AgentFallbackDisposition Disposition,
    string Action,
    string OperatorHint,
    AgentFailureSeverity MinimumSeverity = AgentFailureSeverity.Warning);

public sealed record AgentFailureTelemetrySnapshot(
    IReadOnlyList<AgentFailureEvent> Failures,
    IReadOnlyList<AgentFallbackEvent> Fallbacks,
    IReadOnlyList<AgentFailurePattern> RepeatedPatterns);

public sealed record AgentFailurePattern(
    string PatternKey,
    int Count,
    AgentFailureKind Kind,
    AgentFailureSeverity HighestSeverity,
    string Summary,
    DateTimeOffset LastSeenUtc);

public interface IAgentFailureTelemetryStore
{
    AgentFailureEvent RecordFailure(AgentFailureEvent failure);

    AgentFallbackEvent RecordFallback(AgentFallbackEvent fallback);

    AgentFailureTelemetrySnapshot Snapshot(int recentLimit = 100);
}
