namespace DotNetAgents.LaneOps;

/// <summary>
/// Detects when a lane's base ref has moved far enough to require autonomous
/// recovery, then chooses the least risky recovery posture.
/// </summary>
public sealed class LaneBaseDriftDetector
{
    private readonly ILaneBaseDriftProbe _probe;
    private readonly ILaneBaseDriftAuditSink _auditSink;

    public LaneBaseDriftDetector(
        ILaneBaseDriftProbe probe,
        ILaneBaseDriftAuditSink? auditSink = null)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _auditSink = auditSink ?? NullLaneBaseDriftAuditSink.Instance;
    }

    public async Task<LaneBaseDriftDecision> PollOnceAsync(
        LaneBaseDriftRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var snapshot = await _probe.GetSnapshotAsync(request, cancellationToken).ConfigureAwait(false);
        var triggered = snapshot.CommitsDiverged >= request.Policy.CommitsDivergedThreshold
            || request.LaneAge >= request.Policy.LaneAgeThreshold;

        var decision = triggered
            ? DecideTriggeredDrift(request, snapshot)
            : new LaneBaseDriftDecision(
                LaneBaseDriftDecisionKind.NoDrift,
                LaneBaseDriftRecoveryAction.None,
                snapshot,
                "Base drift is below the configured commit and lane-age thresholds.",
                CadreRunDirective.None);

        var auditRecord = new LaneBaseDriftAuditRecord(
            request.StoryId,
            request.LaneId,
            request.Branch,
            request.BaseRef,
            snapshot.BaseHead,
            snapshot.LaneHead,
            snapshot.MergeBase,
            snapshot.CommitsDiverged,
            snapshot.HasConflicts,
            decision.Kind,
            decision.RecoveryAction,
            decision.CadreRunDirective,
            decision.Reason);

        await _auditSink.RecordAsync(auditRecord, cancellationToken).ConfigureAwait(false);
        return decision;
    }

    private static LaneBaseDriftDecision DecideTriggeredDrift(
        LaneBaseDriftRequest request,
        LaneBaseDriftSnapshot snapshot)
    {
        if (!snapshot.HasConflicts)
        {
            return new LaneBaseDriftDecision(
                LaneBaseDriftDecisionKind.Rebase,
                LaneBaseDriftRecoveryAction.AutoRebase,
                snapshot,
                "Base drift reached threshold and the lane can rebase without conflicts.",
                CadreDirectiveFor(request));
        }

        if (request.StorySize == LaneStorySize.Small)
        {
            return new LaneBaseDriftDecision(
                LaneBaseDriftDecisionKind.Replay,
                LaneBaseDriftRecoveryAction.AutoReplay,
                snapshot,
                "Base drift reached threshold with conflicts, but the story is small enough to replay on the new base.",
                CadreDirectiveFor(request));
        }

        return new LaneBaseDriftDecision(
            LaneBaseDriftDecisionKind.Escalate,
            LaneBaseDriftRecoveryAction.Escalate,
            snapshot,
            "Base drift reached threshold with conflicts on a large story; autonomous semantic conflict resolution is blocked.",
            CadreDirectiveFor(request));
    }

    private static CadreRunDirective CadreDirectiveFor(LaneBaseDriftRequest request) =>
        request.CadreRunActive ? CadreRunDirective.CancelAndRestartAfterRecovery : CadreRunDirective.None;

    private static void Validate(LaneBaseDriftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LaneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Branch);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaseRef);

        if (request.Policy.PollInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(request), "Poll interval must be positive.");
        if (request.Policy.CommitsDivergedThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(request), "Commits-diverged threshold must be at least 1.");
        if (request.Policy.LaneAgeThreshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(request), "Lane-age threshold must be positive.");
    }
}

public interface ILaneBaseDriftProbe
{
    Task<LaneBaseDriftSnapshot> GetSnapshotAsync(
        LaneBaseDriftRequest request,
        CancellationToken cancellationToken = default);
}

public interface ILaneBaseDriftAuditSink
{
    Task RecordAsync(LaneBaseDriftAuditRecord record, CancellationToken cancellationToken = default);
}

public sealed class NullLaneBaseDriftAuditSink : ILaneBaseDriftAuditSink
{
    public static NullLaneBaseDriftAuditSink Instance { get; } = new();

    private NullLaneBaseDriftAuditSink()
    {
    }

    public Task RecordAsync(LaneBaseDriftAuditRecord record, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class InMemoryLaneBaseDriftAuditSink : ILaneBaseDriftAuditSink
{
    private readonly List<LaneBaseDriftAuditRecord> _records = new();

    public int Count => _records.Count;

    public IReadOnlyList<LaneBaseDriftAuditRecord> Snapshot() => _records.ToArray();

    public Task RecordAsync(LaneBaseDriftAuditRecord record, CancellationToken cancellationToken = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }
}

public sealed record LaneBaseDriftRequest(
    string LaneId,
    string Branch,
    string BaseRef,
    LaneBaseDriftPolicy Policy,
    TimeSpan LaneAge,
    LaneStorySize StorySize,
    bool CadreRunActive = false,
    string? StoryId = null);

public sealed record LaneBaseDriftPolicy(
    TimeSpan PollInterval,
    int CommitsDivergedThreshold,
    TimeSpan LaneAgeThreshold)
{
    public static LaneBaseDriftPolicy Default { get; } = new(
        PollInterval: TimeSpan.FromMinutes(5),
        CommitsDivergedThreshold: 1,
        LaneAgeThreshold: TimeSpan.FromHours(2));
}

public sealed record LaneBaseDriftSnapshot(
    string BaseHead,
    string LaneHead,
    string MergeBase,
    int CommitsDiverged,
    bool HasConflicts,
    IReadOnlyList<string>? ConflictPaths = null);

public sealed record LaneBaseDriftDecision(
    LaneBaseDriftDecisionKind Kind,
    LaneBaseDriftRecoveryAction RecoveryAction,
    LaneBaseDriftSnapshot Snapshot,
    string Reason,
    CadreRunDirective CadreRunDirective);

public sealed record LaneBaseDriftAuditRecord(
    string? StoryId,
    string LaneId,
    string Branch,
    string BaseRef,
    string BaseHead,
    string LaneHead,
    string MergeBase,
    int CommitsDiverged,
    bool HasConflicts,
    LaneBaseDriftDecisionKind Decision,
    LaneBaseDriftRecoveryAction RecoveryAction,
    CadreRunDirective CadreRunDirective,
    string Reason);

public enum LaneBaseDriftDecisionKind
{
    NoDrift,
    Rebase,
    Replay,
    Escalate,
}

public enum LaneBaseDriftRecoveryAction
{
    None,
    AutoRebase,
    AutoReplay,
    Escalate,
}

public enum CadreRunDirective
{
    None,
    CancelAndRestartAfterRecovery,
}

public enum LaneStorySize
{
    Small,
    Large,
}
