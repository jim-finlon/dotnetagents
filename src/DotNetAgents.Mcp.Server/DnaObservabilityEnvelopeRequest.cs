namespace DotNetAgents.Mcp.Server;

public sealed record DnaObservabilityEnvelopeRequest(
    string Topic,
    string EventType,
    string SourceService)
{
    public string Severity { get; init; } = "info";
    public string? EventId { get; init; }
    public DateTimeOffset? OccurredAtUtc { get; init; }
    public string? SourceEnvironment { get; init; }
    public string? SourceInstanceId { get; init; }
    public string? SourceVersion { get; init; }
    public string? SourceHost { get; init; }
    public int? SourceProcessId { get; init; }
    public string? ActorType { get; init; }
    public string? ActorId { get; init; }
    public string? ActorDisplayName { get; init; }
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? CausationId { get; init; }
    public string? StoryId { get; init; }
    public string? EpicId { get; init; }
    public string? RunId { get; init; }
    public string? WorkflowRunId { get; init; }
    public string? WorktreePath { get; init; }
    public string? Branch { get; init; }
    public string? CommitSha { get; init; }
    public string? SubjectKind { get; init; }
    public string? SubjectId { get; init; }
    public string? SubjectName { get; init; }
    public IReadOnlyDictionary<string, object?> Metrics { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, object?> Dimensions { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);
    public string? PayloadSummary { get; init; }
    public string PrivacyClass { get; init; } = "internal";
    public string RetentionClass { get; init; } = "standard";
    public string RedactionStatus { get; init; } = "redacted";
    public IReadOnlyList<string> RedactionRules { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
