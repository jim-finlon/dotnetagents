// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using DotNetAgents.Abstractions.Hooks;

namespace DotNetAgents.Core.Hooks;

/// <summary>
/// Captures lifecycle events as evidence entries for later persistence into the SDLC
/// evidence ledger. Always returns Allow — this hook is observe-only and exists to feed
/// the evidence spine (Genetic Code Axiom 2) without altering the agent loop.
/// </summary>
/// <remarks>
/// Evidence capture is in-memory by default; production deployments inject a persistence
/// callback that writes entries to the SDLC contribution_entry ledger. The hook
/// implementation here is the framework primitive; the SDLC integration is wired via
/// the persistence delegate.
/// </remarks>
public sealed class EvidenceCaptureHook : IAgentHook
{
    private static readonly IReadOnlySet<HookCheckpoint> _allCheckpoints =
        new HashSet<HookCheckpoint>(Enum.GetValues<HookCheckpoint>());

    private readonly ConcurrentBag<CapturedEvidenceEntry> _capturedInMemory = new();
    private readonly Func<CapturedEvidenceEntry, CancellationToken, Task>? _persistAsync;

    /// <summary>Default constructor — captures evidence in memory only (test/dev use).</summary>
    public EvidenceCaptureHook()
    {
    }

    /// <summary>Constructor with a persistence callback — production wiring writes to the SDLC evidence ledger.</summary>
    public EvidenceCaptureHook(Func<CapturedEvidenceEntry, CancellationToken, Task> persistAsync)
    {
        _persistAsync = persistAsync ?? throw new ArgumentNullException(nameof(persistAsync));
    }

    public string Id => "dotnetagents.evidence-capture-hook";
    public string DisplayName => "Evidence Capture Hook";
    public IReadOnlySet<HookCheckpoint> SubscribedCheckpoints => _allCheckpoints;
    public int Priority => 90; // Run late so any redactions from earlier hooks have applied

    /// <summary>In-memory snapshot of captured entries (test convenience).</summary>
    public IReadOnlyCollection<CapturedEvidenceEntry> CapturedInMemory => _capturedInMemory.ToArray();

    public async Task<HookDecision> EvaluateAsync(
        AgentHookContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entry = new CapturedEvidenceEntry(
            HookId: Id,
            Checkpoint: context.Checkpoint,
            ActorId: context.ActorId,
            TaskId: context.TaskId,
            PayloadSnippet: SafeSnippet(context.Payload),
            OccurredAtUtc: context.OccurredAtUtc);

        _capturedInMemory.Add(entry);

        if (_persistAsync is not null)
        {
            try
            {
                await _persistAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Persistence failure must not break the agent loop.
                // Real impl emits OTEL warning + a separate evidence entry recording the failure.
            }
        }

        return HookDecision.Allow;
    }

    private static string SafeSnippet(object? payload, int max = 512)
    {
        if (payload is null) return "(null)";
        var text = payload as string ?? payload.ToString() ?? string.Empty;
        return text.Length <= max ? text : text[..max] + "…";
    }
}

/// <summary>One evidence entry captured by <see cref="EvidenceCaptureHook"/>.</summary>
public sealed record CapturedEvidenceEntry(
    string HookId,
    HookCheckpoint Checkpoint,
    string ActorId,
    string? TaskId,
    string PayloadSnippet,
    DateTimeOffset OccurredAtUtc);
