// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Intent;

namespace DotNetAgents.ContextIntent;

/// <summary>
/// Bridges <see cref="AgentDispatchIntent"/> and <see cref="ContextIntentEnvelope"/> (story 305dd821).
/// </summary>
public static class AgentDispatchIntentBridge
{
    public static ContextIntentEnvelope ToEnvelope(
        AgentDispatchIntent intent,
        string taskId,
        ProvenanceSpec provenance,
        IReadOnlyList<ContextLayer>? contextLayers = null,
        IReadOnlyList<string>? constraints = null,
        IReadOnlyList<string>? acceptance = null)
    {
        var goal = string.IsNullOrWhiteSpace(intent.RawText)
            ? intent.FullName
            : intent.RawText!;

        var layers = contextLayers?.ToList() ?? [];
        if (layers.Count == 0)
        {
            layers.Add(new ContextLayer
            {
                Id = "dispatch.parameters",
                Source = "voice-or-mcp",
                Scope = ContextLayerScope.Turn,
                Content = intent.Parameters
            });
        }

        return new ContextIntentEnvelope
        {
            TaskId = taskId,
            Intent = new IntentSpec(
                Verb: intent.ToContextVerb(),
                Goal: goal,
                SuccessCriteria: intent.IsComplete ? null : intent.MissingRequired),
            ContextLayers = layers,
            Constraints = constraints,
            Acceptance = acceptance,
            Provenance = provenance
        };
    }

    public static AgentDispatchIntent FromEnvelope(ContextIntentEnvelope envelope)
    {
        var verb = envelope.Intent.Verb;
        var parts = verb.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var domain = parts.Length > 0 ? parts[0] : "unknown";
        var action = parts.Length > 1 ? parts[1] : "execute";
        string? subType = parts.Length > 2 ? string.Join('.', parts.Skip(2)) : null;

        var parameters = envelope.ContextLayers
            .FirstOrDefault(layer => string.Equals(layer.Id, "dispatch.parameters", StringComparison.Ordinal))
            ?.Content as IReadOnlyDictionary<string, object>;

        return new AgentDispatchIntent
        {
            Domain = domain,
            Action = action,
            SubType = subType,
            Parameters = parameters is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(parameters),
            MissingRequired = envelope.Intent.SuccessCriteria?.ToList() ?? [],
            RawText = envelope.Intent.Goal
        };
    }
}
