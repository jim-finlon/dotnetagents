using System.Collections.ObjectModel;

namespace DotNetAgents.Runtime;

public enum DelegatedAgentRunStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    TimedOut
}

public sealed record DelegatedAgentRunRequest
{
    public string ParentSessionId { get; init; } = string.Empty;
    public string ParentActorId { get; init; } = string.Empty;
    public string ChildActorId { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public IReadOnlySet<string> AllowedTools { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> DeniedTools { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public int? BudgetTokens { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public int CurrentDepth { get; init; }
    public int MaxDepth { get; init; } = 1;
    public ModelRouteMetadata ModelRoute { get; init; } = new("fake", "delegated-test-model");
    public IReadOnlyList<ArtifactReference> ArtifactRefs { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
}

public sealed record DelegatedAgentRun
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public string ParentSessionId { get; init; } = string.Empty;
    public string? ChildSessionId { get; init; }
    public string ParentActorId { get; init; } = string.Empty;
    public string ChildActorId { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public IReadOnlySet<string> AllowedTools { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> DeniedTools { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public int? BudgetTokens { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public int CurrentDepth { get; init; }
    public int MaxDepth { get; init; } = 1;
    public DelegatedAgentRunStatus Status { get; init; } = DelegatedAgentRunStatus.Pending;
    public string? ResultSummary { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TrajectoryId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public IReadOnlyList<ArtifactReference> ArtifactRefs { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
}

public sealed record DelegationPolicyDecision(bool Allowed, string? Reason = null)
{
    public static DelegationPolicyDecision Permit() => new(true);

    public static DelegationPolicyDecision Deny(string reason) => new(false, reason);
}

public sealed record DelegatedAgentRunResult(
    DelegatedAgentRun Run,
    string Summary,
    IReadOnlyList<ArtifactReference> ArtifactRefs,
    string? TrajectoryId);
