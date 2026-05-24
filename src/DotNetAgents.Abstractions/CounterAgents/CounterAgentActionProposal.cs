// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.CounterAgents;

/// <summary>
/// A proposed action that a counter-agent will review before it is executed.
/// </summary>
/// <param name="ActorId">Stable id of the agent or operator about to act (e.g. "agent-host", "jarvis", "agent-alpha").</param>
/// <param name="ActionType">Category of action — typical values: "tool_call", "deploy", "story_close", "code_review_submit", "credential_rotate". Free-form so callers can extend.</param>
/// <param name="ActionName">Specific name within the type — e.g. tool name, deployment id, story id.</param>
/// <param name="Input">The action's input payload (serializable). Pass <c>null</c> for void-input actions.</param>
/// <param name="Metadata">Free-form structured context counter-agents can read (cost estimate, posture, target, etc.). Conventional keys: "cost_estimate_usd", "estimated_input_tokens", "estimated_output_tokens", "story_id", "risk_level".</param>
/// <remarks>
/// The proposal is *immutable*. Counter-agents are review-only — if a counter-agent wants to
/// modify the action, it must Block and the agent loop creates a new proposal.
/// </remarks>
public sealed record CounterAgentActionProposal(
    string ActorId,
    string ActionType,
    string ActionName,
    object? Input,
    IReadOnlyDictionary<string, object>? Metadata = null)
{
    /// <summary>
    /// Convenience: get a metadata value by key with type-safe coercion. Returns
    /// <paramref name="defaultValue"/> when the key is missing or the value cannot be coerced.
    /// </summary>
#pragma warning disable CA1721 // GetMetadataValue and Metadata property are intentionally complementary by name
    public T? GetMetadataValue<T>(string key, T? defaultValue = default)
#pragma warning restore CA1721
    {
        if (Metadata is null) return defaultValue;
        if (!Metadata.TryGetValue(key, out var value)) return defaultValue;
        if (value is T typed) return typed;
        try
        {
            return (T)Convert.ChangeType(value, typeof(T))!;
        }
        catch
        {
            return defaultValue;
        }
    }
}
