// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace DotNetAgents.LaneOps;

/// <summary>
/// Story 97a623fd. DotNetAgents wrapper around scripts/recover-dna-agent-worktree.sh.
/// </summary>
public interface IRecoveryHarness
{
    Task<RecoveryAttemptResult> VerifyAsync(RecoveryHarnessRequest request, CancellationToken cancellationToken = default);

    Task<RecoveryApplyResult> ApplyAsync(RecoveryHarnessRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Request envelope for one recovery-harness invocation.</summary>
public sealed record RecoveryHarnessRequest(
    string WorktreePath,
    string MainCheckoutPath,
    string? Branch = null,
    string? StoryId = null,
    string? LaneId = null,
    string? ActorId = null,
    string? ScriptPath = null,
    string? BashExecutable = null,
    string? MergedRef = null,
    string? SdlcMcpUrl = null,
    string? PmaMcpUrl = null,
    string? SdlcMcpAuthHeader = null,
    string? PmaMcpAuthHeader = null,
    string? SdlcApiKey = null,
    int? McpTimeoutSec = null,
    bool AllowDirty = false,
    bool SkipMergedCheck = false,
    bool SkipFetch = false,
    IReadOnlyList<string>? WorktreesRoots = null);

/// <summary>Result of one verify/apply script invocation.</summary>
public sealed record RecoveryAttemptResult(
    RecoveryAttemptMode Mode,
    RecoveryScriptReport? Report,
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded => ExitCode == 0 && (Report?.Ok ?? false);
}

/// <summary>Apply runs a preflight verify plus the actual apply invocation.</summary>
public sealed record RecoveryApplyResult(
    RecoveryAttemptResult VerifyAttempt,
    RecoveryAttemptResult? ApplyAttempt)
{
    public bool Succeeded => VerifyAttempt.Succeeded && ApplyAttempt is { Succeeded: true };
}

public enum RecoveryAttemptMode
{
    Verify,
    Apply,
}

/// <summary>Typed projection of the recovery shell script's JSON response.</summary>
public sealed record RecoveryScriptReport(
    string WorktreePath,
    string Branch,
    string StoryId,
    string MergedRef,
    RecoveryVerifyState Verify,
    RecoverySoftSignals SoftSignals,
    IReadOnlyList<string> SiblingPaths,
    IReadOnlyList<string> Warnings,
    bool Ok,
    string RefusalReason,
    bool Applied,
    string Mode);

public sealed record RecoveryVerifyState(
    [property: JsonPropertyName("worktree.present")] bool WorktreePresent,
    [property: JsonPropertyName("worktree.dirty")] bool WorktreeDirty,
    [property: JsonPropertyName("branch.local")] bool BranchLocal,
    [property: JsonPropertyName("branch.merged")] bool BranchMerged,
    [property: JsonPropertyName("remote.present")] bool RemotePresent,
    [property: JsonPropertyName("siblings.share_parent")] bool SiblingsShareParent,
    [property: JsonPropertyName("canonical.protected")] bool CanonicalProtected);

public sealed record RecoverySoftSignals(
    [property: JsonPropertyName("lease.records.present")] string LeaseRecordsPresent,
    [property: JsonPropertyName("cleanup.receipt.present")] string CleanupReceiptPresent,
    string WorkOrderId);
