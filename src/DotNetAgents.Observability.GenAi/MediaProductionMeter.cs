using System.Diagnostics.Metrics;

namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Shared <see cref="Meter"/> for Sora-Parity v1 media-production metrics (FR-SP704).
/// Story edf42bd1 (P7.5 T4). Instruments are exposed as named constants so dashboards and
/// the OTel collector pipeline can pin on them; the <see cref="Meter"/> instance is process-
/// singleton.
/// </summary>
/// <remarks>
/// Operators wire OTel exporters by listening on <see cref="Name"/>; that surface name is
/// the same one used by spans emitted under
/// <see cref="GenAiActivitySource"/> for media activities, so dashboard correlation is
/// straightforward.
/// </remarks>
public static class MediaProductionMeter
{
    /// <summary>Canonical meter / activity-source name.</summary>
    public const string Name = "DNA.Genetic.MediaProduction";

    /// <summary>Continuity score histogram instrument (FR-SP704). Recorded per shot boundary in [0, 1].</summary>
    public const string ContinuityScoreInstrument = "dna.media.continuity.score";

    /// <summary>LoRA training duration histogram instrument (FR-SP704). Recorded in milliseconds at training completion.</summary>
    public const string LoraTrainingDurationInstrument = "dna.media.lora.training.duration_ms";

    /// <summary>Fleet slot utilization gauge instrument (FR-SP704). Recorded per provider-surface poll, in [0, 1] per slot.</summary>
    public const string FleetSlotUtilizationInstrument = "dna.media.fleet.slot.utilization";

    private static readonly Meter Instance = new(Name);
    private static readonly Histogram<double> _continuityScore = Instance.CreateHistogram<double>(
        ContinuityScoreInstrument,
        unit: "{score}",
        description: "Boundary continuity score in [0, 1] per shot boundary. Recorded by IContinuityValidator implementations.");
    private static readonly Histogram<long> _loraTrainingDuration = Instance.CreateHistogram<long>(
        LoraTrainingDurationInstrument,
        unit: "ms",
        description: "End-to-end LoRA training duration in milliseconds, recorded at training completion.");
    private static readonly Histogram<double> _fleetSlotUtilization = Instance.CreateHistogram<double>(
        FleetSlotUtilizationInstrument,
        unit: "{ratio}",
        description: "Per-slot utilization ratio in [0, 1] recorded at each provider-surface poll.");

    /// <summary>Record one continuity-score sample with optional attributes.</summary>
    public static void RecordContinuityScore(double score, params KeyValuePair<string, object?>[] tags) =>
        _continuityScore.Record(score, tags);

    /// <summary>Record one LoRA-training-duration sample with optional attributes.</summary>
    public static void RecordLoraTrainingDuration(long durationMs, params KeyValuePair<string, object?>[] tags) =>
        _loraTrainingDuration.Record(durationMs, tags);

    /// <summary>Record one fleet-slot-utilization sample with optional attributes.</summary>
    public static void RecordFleetSlotUtilization(double ratio, params KeyValuePair<string, object?>[] tags) =>
        _fleetSlotUtilization.Record(ratio, tags);

    /// <summary>Direct access to the meter for callers that want to attach their own instruments.</summary>
    public static Meter SharedMeter => Instance;
}
