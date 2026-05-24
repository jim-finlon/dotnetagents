// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.PromptRuntime;

/// <summary>
/// Request to resolve a prompt body from PromptSpecialist (or the local fallback) and render it
/// with supplied variable values.
/// </summary>
/// <param name="Key">Durable prompt key (domain.agent.purpose, 3-5 segments).</param>
/// <param name="Variables">Values for declared {{name}} placeholders. Null/empty = no substitution.</param>
/// <param name="AllocationKey">Optional stable key for A/B allocation (user, session, etc.).</param>
/// <param name="IncludeMetadata">When true, PromptSpecialist returns metadata (score, fitness, lineage, contracts).</param>
/// <param name="Timeout">Optional per-request timeout; defaults to the client-level timeout.</param>
public sealed record PromptRequest(
    string Key,
    IReadOnlyDictionary<string, string>? Variables = null,
    string? AllocationKey = null,
    bool IncludeMetadata = false,
    TimeSpan? Timeout = null);
