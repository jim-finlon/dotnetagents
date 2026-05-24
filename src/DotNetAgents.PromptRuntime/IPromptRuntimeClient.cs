// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.PromptRuntime;

/// <summary>
/// Host-side client for the PromptSpecialist runtime. Default implementation is HTTP-backed and
/// falls back to the local <see cref="IPromptRegistry"/> when PromptSpecialist is unreachable,
/// returning 404, or explicitly disabled via options — so critical callsites keep functioning
/// even if the library service is down.
/// </summary>
public interface IPromptRuntimeClient
{
    /// <summary>Resolve + render a prompt body.</summary>
    Task<PromptResult> ResolveAsync(PromptRequest request, CancellationToken ct = default);

    /// <summary>Report the downstream outcome (success/failure + optional quality score) so the library
    /// can update per-variant fitness. Best-effort: failures are swallowed, not raised.</summary>
    Task ReportOutcomeAsync(PromptOutcomeReport report, CancellationToken ct = default);
}

/// <param name="Key">Prompt key the outcome is about.</param>
/// <param name="VariantId">Variant id returned by ResolveAsync. Null when the body came from local fallback.</param>
/// <param name="Success">True when the task succeeded.</param>
/// <param name="QualityScore">Optional 0..1 quality signal (rubric score, downstream judge result, etc.).</param>
/// <param name="TaskId">Optional caller-supplied task id for correlation.</param>
/// <param name="IdempotencyKey">Optional idempotency key to dedup reports.</param>
public sealed record PromptOutcomeReport(
    string Key,
    Guid? VariantId,
    bool Success,
    double? QualityScore = null,
    string? TaskId = null,
    string? IdempotencyKey = null);
