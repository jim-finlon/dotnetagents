// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Intent;

/// <summary>Cross-surface helpers for <see cref="AgentDispatchIntent"/> (story 305dd821).</summary>
public static class AgentDispatchIntentExtensions
{
    /// <summary>
    /// Clone intent with merged parameters for MCP execution (orchestration correlation keys, etc.).
    /// </summary>
    public static AgentDispatchIntent WithParameters(
        this AgentDispatchIntent intent,
        IReadOnlyDictionary<string, object> parameters)
    {
        var merged = new Dictionary<string, object>(intent.Parameters, StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
            merged[key] = value;

        return intent with { Parameters = merged };
    }

    /// <summary>Map domain/action/subtype to a ContextIntent verb alias.</summary>
    public static string ToContextVerb(this AgentDispatchIntent intent) =>
        string.IsNullOrEmpty(intent.SubType)
            ? $"{intent.Domain}.{intent.Action}"
            : $"{intent.Domain}.{intent.Action}.{intent.SubType}";
}
