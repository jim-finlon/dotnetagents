namespace DotNetAgents.Agents.Tasks;

/// <summary>
/// Stable disposition vocabulary for control-loop work. Queue implementations can
/// keep their own storage model while projecting into these buckets for operators.
/// </summary>
public enum QueueWorkDisposition
{
    Unknown = 0,
    Pending = 1,
    Active = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

/// <summary>
/// Operator-facing queue posture. This is deliberately compact so dashboards can
/// compare workflow, reactive-policy, and state-machine queues without payload access.
/// </summary>
public enum QueueBackpressureState
{
    Unknown = 0,
    Empty = 1,
    Normal = 2,
    Constrained = 3,
    Saturated = 4
}

/// <summary>Shared retry budget metadata for queued work.</summary>
public sealed record RetryBudget(
    int Attempts,
    int MaxAttempts,
    DateTimeOffset? NextRetryAtUtc = null)
{
    public int RemainingAttempts => Math.Max(0, MaxAttempts - Attempts);
}

/// <summary>Identity and lineage fields that are safe to project into control-loop views.</summary>
public sealed record QueueWorkItemIdentity(
    string WorkItemId,
    string WorkItemType,
    string? QueueKey = null,
    string? CorrelationId = null,
    string? StoryId = null,
    string? ParentWorkItemId = null);

/// <summary>Payload-free view of one work item as it moves through queue/dispatch states.</summary>
public sealed record QueueWorkItemSnapshot(
    QueueWorkItemIdentity Identity,
    QueueWorkDisposition Disposition,
    RetryBudget RetryBudget,
    int Priority,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EnqueuedAtUtc = null,
    DateTimeOffset? StartedAtUtc = null,
    DateTimeOffset? CompletedAtUtc = null);

/// <summary>Aggregated queue posture for control loops and dashboards.</summary>
public sealed record QueuePostureSnapshot(
    string QueueKey,
    QueueBackpressureState Backpressure,
    int PendingDepth,
    int ActiveCount,
    int CompletedCount,
    int FailedCount,
    int RetryingCount,
    int? MaxDepthBudget = null,
    DateTimeOffset? OldestPendingUtc = null,
    IReadOnlyList<QueueWorkItemSnapshot>? WorkItems = null)
{
    public bool HasRetryPressure => RetryingCount > 0 || WorkItems?.Any(static item => item.RetryBudget.Attempts > 0) == true;
}
