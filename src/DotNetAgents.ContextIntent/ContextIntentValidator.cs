// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ContextIntent;

/// <summary>
/// Validates a <see cref="ContextIntentEnvelope"/> against the v1 schema requirements.
/// Returns a structured <see cref="ContextIntentValidationResult"/> rather than throwing —
/// callers can decide whether validation failure should warn or block based on the configured
/// <see cref="ContextIntentEnforcementMode"/>.
/// </summary>
public sealed class ContextIntentValidator
{
    /// <summary>Maximum number of context layers per the v1 schema.</summary>
    public const int MaxContextLayers = 32;

    /// <summary>Validate the envelope. Never throws — returns a result regardless of input shape.</summary>
    public ContextIntentValidationResult Validate(ContextIntentEnvelope? envelope)
    {
        if (envelope is null)
        {
            return ContextIntentValidationResult.Failure(new[] { "Envelope is null." });
        }

        var errors = new List<string>();

        if (envelope.SchemaVersion != ContextIntentEnvelope.V1SchemaVersion)
        {
            errors.Add($"schema_version must be '{ContextIntentEnvelope.V1SchemaVersion}'; got '{envelope.SchemaVersion}'.");
        }

        if (string.IsNullOrWhiteSpace(envelope.TaskId))
        {
            errors.Add("task_id is required and must be non-empty.");
        }

        ValidateIntent(envelope.Intent, errors);
        ValidateContextLayers(envelope.ContextLayers, errors);
        ValidateProvenance(envelope.Provenance, errors);

        return errors.Count == 0
            ? ContextIntentValidationResult.Success()
            : ContextIntentValidationResult.Failure(errors);
    }

    private static void ValidateIntent(IntentSpec? intent, List<string> errors)
    {
        if (intent is null)
        {
            errors.Add("intent is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(intent.Verb))
        {
            errors.Add("intent.verb is required and must be non-empty.");
        }

        if (string.IsNullOrWhiteSpace(intent.Goal))
        {
            errors.Add("intent.goal is required and must be non-empty.");
        }
    }

    private static void ValidateContextLayers(IReadOnlyList<ContextLayer>? layers, List<string> errors)
    {
        if (layers is null)
        {
            errors.Add("context_layers is required.");
            return;
        }

        if (layers.Count > MaxContextLayers)
        {
            errors.Add($"context_layers exceeds maximum of {MaxContextLayers} (got {layers.Count}).");
        }

        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            var prefix = $"context_layers[{i}]";
            if (string.IsNullOrWhiteSpace(layer.Id))
            {
                errors.Add($"{prefix}.id is required.");
            }
            if (string.IsNullOrWhiteSpace(layer.Source))
            {
                errors.Add($"{prefix}.source is required.");
            }
            if (layer.Content is null)
            {
                errors.Add($"{prefix}.content is required.");
            }
        }

        var ids = layers.Where(l => !string.IsNullOrWhiteSpace(l.Id)).Select(l => l.Id).ToList();
        var duplicateIds = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicateIds.Length > 0)
        {
            errors.Add($"context_layers contains duplicate ids: {string.Join(", ", duplicateIds)}.");
        }
    }

    private static void ValidateProvenance(ProvenanceSpec? provenance, List<string> errors)
    {
        if (provenance is null)
        {
            errors.Add("provenance is required.");
            return;
        }

        if (provenance.Actor is null)
        {
            errors.Add("provenance.actor is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(provenance.Actor.ActorType))
            {
                errors.Add("provenance.actor.actor_type is required.");
            }
            if (string.IsNullOrWhiteSpace(provenance.Actor.ActorId))
            {
                errors.Add("provenance.actor.actor_id is required.");
            }
        }

        if (provenance.CapturedAt == default)
        {
            errors.Add("provenance.captured_at is required.");
        }
    }
}

/// <summary>The outcome of validating a <see cref="ContextIntentEnvelope"/>.</summary>
public sealed record ContextIntentValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ContextIntentValidationResult Success() =>
        new(IsValid: true, Errors: Array.Empty<string>());

    public static ContextIntentValidationResult Failure(IEnumerable<string> errors) =>
        new(IsValid: false, Errors: errors.ToArray());
}
