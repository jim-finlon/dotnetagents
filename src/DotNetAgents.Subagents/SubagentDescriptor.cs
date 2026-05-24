// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Subagents;

/// <summary>
/// Declarative description of a subagent — name, instructions, allowed tools, optional model
/// override, optional max-tokens cap. Subagents are short-lived ephemeral helpers spawned by
/// a parent agent for a focused subtask; each runs with its own context window so the parent's
/// context is preserved.
/// </summary>
/// <param name="Name">Stable identifier (used for telemetry + cost attribution).</param>
/// <param name="Instructions">System-prompt-equivalent instructions for the subagent.</param>
/// <param name="AllowedTools">Tool name allowlist — the subagent cannot call tools outside this list. Empty list = no tools.</param>
/// <param name="ModelOverride">Optional model id. When null, the subagent uses the parent's configured model.</param>
/// <param name="MaxTokens">Optional max-tokens cap for the subagent's response. When null, the runner's default applies.</param>
/// <param name="MaxDepth">Maximum nesting depth — a subagent at <paramref name="MaxDepth"/>=0 cannot spawn its own subagents. Default 3 to prevent runaway recursion.</param>
/// <param name="TimeoutSeconds">Maximum wall-clock seconds for the subagent's execution. Default 60s.</param>
public sealed record SubagentDescriptor(
    string Name,
    string Instructions,
    IReadOnlyList<string> AllowedTools,
    string? ModelOverride = null,
    int? MaxTokens = null,
    int MaxDepth = 3,
    int TimeoutSeconds = 60);
