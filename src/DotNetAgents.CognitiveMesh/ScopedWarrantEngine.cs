// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.CognitiveMesh;

/// <summary>
/// Deterministic rule engine for scoped warrant status transitions. It validates metadata presence
/// and transition shape only; it does not call LLMs, juries, persistence, or external services.
/// </summary>
public sealed class ScopedWarrantEngine
{
    private static readonly IReadOnlyDictionary<ClaimWarrantStatus, int> WarrantRank =
        new Dictionary<ClaimWarrantStatus, int>
        {
            [ClaimWarrantStatus.Captured] = 0,
            [ClaimWarrantStatus.Held] = 1,
            [ClaimWarrantStatus.InternallyConsistent] = 2,
            [ClaimWarrantStatus.EvidenceLinked] = 3,
            [ClaimWarrantStatus.JurySupported] = 4,
            [ClaimWarrantStatus.ExternallyCoupled] = 5,
            [ClaimWarrantStatus.OperationallyCommitted] = 6
        };

    /// <summary>
    /// Evaluates whether a scoped claim can move from one warrant status to another.
    /// </summary>
    public WarrantTransitionResult CanTransition(
        ClaimWarrantStatus from,
        ClaimWarrantStatus to,
        WarrantTransitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (from == to)
        {
            return WarrantTransitionResult.Allowed("no_op", "The claim is already in the requested warrant status.");
        }

        if (IsTerminalDisposition(to))
        {
            return ValidateTerminalDisposition(to, context);
        }

        if (IsTerminalDisposition(from))
        {
            var reopen = ValidateReopen(from, to, context);
            if (!reopen.IsAllowed)
            {
                return reopen;
            }

            var reopenMetadata = ValidateIncreaseMetadata(context);
            return reopenMetadata.IsAllowed
                ? reopen
                : reopenMetadata;
        }

        if (!WarrantRank.TryGetValue(from, out var fromRank) ||
            !WarrantRank.TryGetValue(to, out var toRank))
        {
            return WarrantTransitionResult.Rejected(
                "unsupported_status",
                $"Transition from {from} to {to} is not part of the progressive warrant lattice.");
        }

        if (toRank != fromRank + 1)
        {
            return WarrantTransitionResult.Rejected(
                "unsupported_jump",
                $"Transition from {from} to {to} skips required intermediate warrant status.");
        }

        var metadata = ValidateIncreaseMetadata(context);
        if (!metadata.IsAllowed)
        {
            return metadata;
        }

        return to switch
        {
            ClaimWarrantStatus.JurySupported => ValidateJurySupport(context),
            ClaimWarrantStatus.ExternallyCoupled => ValidateBridgeAttempt(context),
            ClaimWarrantStatus.OperationallyCommitted => ValidateOperationalCommit(to, context),
            _ => WarrantTransitionResult.Allowed("transition_allowed", $"Transition from {from} to {to} is allowed.")
        };
    }

    private static WarrantTransitionResult ValidateIncreaseMetadata(WarrantTransitionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Scope))
        {
            return WarrantTransitionResult.Rejected("scope_required", "Increasing warrant requires a non-empty scope.");
        }

        if (context.EvidenceRefs.Count == 0)
        {
            return WarrantTransitionResult.Rejected("evidence_required", "Increasing warrant requires at least one evidence reference.");
        }

        if (context.Residues.Count == 0)
        {
            return WarrantTransitionResult.Rejected("residue_required", "Increasing warrant requires at least one residue record.");
        }

        if (string.IsNullOrWhiteSpace(context.ActorRole))
        {
            return WarrantTransitionResult.Rejected("actor_role_required", "Increasing warrant requires the responsible actor role.");
        }

        if (context.OccurredAtUtc == default)
        {
            return WarrantTransitionResult.Rejected("timestamp_required", "Increasing warrant requires a transition timestamp.");
        }

        return WarrantTransitionResult.Allowed("metadata_complete", "Required transition metadata is present.");
    }

    private static WarrantTransitionResult ValidateJurySupport(WarrantTransitionContext context)
    {
        if (context.JuryVerdict is not { Supported: true })
        {
            return WarrantTransitionResult.Rejected("jury_support_required", "JurySupported requires a supporting jury verdict.");
        }

        return WarrantTransitionResult.Allowed("transition_allowed", "Supporting jury verdict is present.");
    }

    private static WarrantTransitionResult ValidateBridgeAttempt(WarrantTransitionContext context)
    {
        if (context.BridgeAttempts.Count == 0)
        {
            return WarrantTransitionResult.Rejected("bridge_attempt_required", "ExternallyCoupled requires at least one bridge attempt.");
        }

        return WarrantTransitionResult.Allowed("transition_allowed", "Bridge attempt evidence is present.");
    }

    private static WarrantTransitionResult ValidateOperationalCommit(
        ClaimWarrantStatus targetStatus,
        WarrantTransitionContext context)
    {
        if (context.CollapseDecision?.ResultStatus != targetStatus)
        {
            return WarrantTransitionResult.Rejected(
                "collapse_decision_required",
                "OperationallyCommitted requires a collapse decision targeting the requested status.");
        }

        return WarrantTransitionResult.Allowed("transition_allowed", "Collapse decision is present.");
    }

    private static WarrantTransitionResult ValidateTerminalDisposition(
        ClaimWarrantStatus to,
        WarrantTransitionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.ActorRole) || context.OccurredAtUtc == default)
        {
            return WarrantTransitionResult.Rejected(
                "terminal_metadata_required",
                $"{to} requires actor role and timestamp metadata.");
        }

        if (context.Residues.Count == 0)
        {
            return WarrantTransitionResult.Rejected("terminal_residue_required", $"{to} requires a residue record.");
        }

        return WarrantTransitionResult.Allowed("terminal_transition_allowed", $"{to} terminal disposition is allowed.");
    }

    private static WarrantTransitionResult ValidateReopen(
        ClaimWarrantStatus from,
        ClaimWarrantStatus to,
        WarrantTransitionContext context)
    {
        if (to is not ClaimWarrantStatus.Held and not ClaimWarrantStatus.InternallyConsistent)
        {
            return WarrantTransitionResult.Rejected(
                "unsupported_reopen_target",
                $"Reopening from {from} can only return to Held or InternallyConsistent.");
        }

        if (string.IsNullOrWhiteSpace(context.ReopenCondition))
        {
            return WarrantTransitionResult.Rejected("reopen_condition_required", "Reopening a terminal warrant requires a reopen condition.");
        }

        return WarrantTransitionResult.Allowed("reopen_allowed", "Reopen condition is present.");
    }

    private static bool IsTerminalDisposition(ClaimWarrantStatus status)
        => status is ClaimWarrantStatus.Superseded
            or ClaimWarrantStatus.Falsified
            or ClaimWarrantStatus.DisciplinedDeparture;
}

/// <summary>
/// Evidence bundle supplied for a proposed scoped warrant transition.
/// </summary>
public sealed record WarrantTransitionContext
{
    public required string Scope { get; init; }
    public IReadOnlyList<EvidenceRef> EvidenceRefs { get; init; } = Array.Empty<EvidenceRef>();
    public IReadOnlyList<ResidueRecord> Residues { get; init; } = Array.Empty<ResidueRecord>();
    public IReadOnlyList<BridgeAttempt> BridgeAttempts { get; init; } = Array.Empty<BridgeAttempt>();
    public required string ActorRole { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public JuryVerdict? JuryVerdict { get; init; }
    public CollapseDecision? CollapseDecision { get; init; }
    public string? ReopenCondition { get; init; }
}

/// <summary>
/// Deterministic verdict for a proposed scoped warrant transition.
/// </summary>
public sealed record WarrantTransitionResult(bool IsAllowed, string Code, string Message)
{
    public static WarrantTransitionResult Allowed(string code, string message)
        => new(true, code, message);

    public static WarrantTransitionResult Rejected(string code, string message)
        => new(false, code, message);
}
