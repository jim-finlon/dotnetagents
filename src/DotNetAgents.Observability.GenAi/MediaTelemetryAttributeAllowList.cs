// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Allow-list of OTel attribute keys that may attach to spans / metrics on the
/// <see cref="MediaProductionMeter"/> surface. Story edf42bd1 (P7.5 T4). The list pairs
/// the FR-SP704 keys (<see cref="MediaProductionAttributeNames"/>) with a small set of
/// operator-safe OTel resource keys; everything else MUST be rejected so secret material
/// can never accidentally leak into telemetry.
/// </summary>
/// <remarks>
/// Forbidden patterns (per the FR-SP704 SecurityNotes): credential names, bearer tokens,
/// X-API-Key values, voice-note transcripts, PII. The allow-list approach refuses unknown
/// keys outright instead of trying to detect secrets by pattern.
/// </remarks>
public static class MediaTelemetryAttributeAllowList
{
    /// <summary>Allow-listed attribute keys. Case-sensitive — match the constant exactly.</summary>
    public static readonly IReadOnlySet<string> AllowedKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        MediaProductionAttributeNames.LoraId,
        MediaProductionAttributeNames.LoraKind,
        MediaProductionAttributeNames.CharacterId,
        MediaProductionAttributeNames.StoryboardId,
        MediaProductionAttributeNames.ShotIndex,
        MediaProductionAttributeNames.GatewayHost,
        MediaProductionAttributeNames.ContinuityScore,
        MediaProductionAttributeNames.QualityTier,
        MediaProductionAttributeNames.ToolName,
        MediaProductionAttributeNames.ActorId,
    };

    /// <summary>True when <paramref name="key"/> is allowed on a media-production telemetry surface.</summary>
    public static bool IsAllowed(string key) => !string.IsNullOrEmpty(key) && AllowedKeys.Contains(key);

    /// <summary>
    /// Filter a tag collection down to the allow-listed pairs. Disallowed keys are dropped
    /// silently so a misconfigured caller cannot leak secrets into the metric backend.
    /// </summary>
    public static KeyValuePair<string, object?>[] Filter(IEnumerable<KeyValuePair<string, object?>> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return tags.Where(t => IsAllowed(t.Key)).ToArray();
    }
}
