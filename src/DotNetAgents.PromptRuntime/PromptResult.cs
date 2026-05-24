// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.PromptRuntime;

/// <summary>
/// Result of a prompt resolution. <see cref="Source"/> reports where the body came from so callers
/// can log "served from remote library" vs "fell back to compile-time default".
/// </summary>
/// <param name="Key">Resolved prompt key.</param>
/// <param name="Text">Rendered prompt body with variables substituted.</param>
/// <param name="VariantId">Variant id returned by PromptSpecialist; null when served from local fallback.</param>
/// <param name="Version">Version number; null when served from local fallback.</param>
/// <param name="Source">Origin of the body.</param>
/// <param name="FitnessScore">Current fitness score from PromptSpecialist; null when not requested or not served remotely.</param>
/// <param name="UnresolvedPlaceholders">Declared placeholders we couldn't substitute.</param>
/// <param name="InstructionBinding">Optional binding that relates this prompt to chain and skill contracts.</param>
public sealed record PromptResult(
    string Key,
    string Text,
    Guid? VariantId,
    int? Version,
    PromptResultSource Source,
    double? FitnessScore = null,
    IReadOnlyList<string>? UnresolvedPlaceholders = null,
    PromptInstructionBinding? InstructionBinding = null)
{
    public bool CameFromRemote => Source == PromptResultSource.RemoteLibrary;
}

public sealed record PromptInstructionBinding(
    string? InstructionArtifactRef,
    IReadOnlyList<string> ChainContractRefs,
    IReadOnlyList<string> SkillRefs);

public enum PromptResultSource
{
    /// <summary>Served by PromptSpecialist over HTTP/MCP.</summary>
    RemoteLibrary = 0,
    /// <summary>PromptSpecialist was unreachable or returned 404 — body came from a compile-time default.</summary>
    LocalFallback = 1,
    /// <summary>Served from a short-lived in-process cache (saves round-trips for hot keys).</summary>
    Cached = 2,
}
