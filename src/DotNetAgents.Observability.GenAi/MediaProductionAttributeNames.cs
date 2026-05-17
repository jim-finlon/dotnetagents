namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Canonical OTel attribute names for the Sora-Parity v1 media-production tool surface
/// (FR-SP704). Story edf42bd1 (P7.5 T4). Implementations MUST use these exact strings;
/// dashboards and the <see cref="MediaTelemetryAttributeAllowList"/> are keyed on them.
/// </summary>
/// <remarks>
/// These attributes attach to spans/metrics emitted under <see cref="MediaProductionMeter.Name"/>.
/// Adding a new key requires (a) adding it here, (b) extending the allow-list, and (c) updating
/// docs/sora-parity-v1/REQUIREMENTS.md FR-SP704.
/// </remarks>
public static class MediaProductionAttributeNames
{
    /// <summary>LoRA weight id active on this call (FR-SP704). Cardinality bounded by trained LoRA count.</summary>
    public const string LoraId = "media.lora.id";

    /// <summary>LoRA kind (CharacterIc / CharacterFast / Style / Motion / IcFusionPair). Low-cardinality.</summary>
    public const string LoraKind = "media.lora.kind";

    /// <summary>Character id this call conditions or trains (FR-SP704).</summary>
    public const string CharacterId = "media.character.id";

    /// <summary>Storyboard id this call is part of (FR-SP704).</summary>
    public const string StoryboardId = "media.storyboard.id";

    /// <summary>Shot index within the storyboard (FR-SP704). 0-based.</summary>
    public const string ShotIndex = "media.shot.index";

    /// <summary>Gateway host id that served the call (FR-SP704). Low-cardinality (one of the configured fleet hosts).</summary>
    public const string GatewayHost = "media.gateway.host";

    /// <summary>Boundary continuity score [0, 1] produced by the continuity verifier (FR-SP704).</summary>
    public const string ContinuityScore = "media.continuity.score";

    /// <summary>QualityTier of the render (Draft / Standard / High / Cinema). Low-cardinality.</summary>
    public const string QualityTier = "media.quality.tier";

    /// <summary>MCP tool name servicing the call (e.g. <c>generate_test_clip</c>). Low-cardinality.</summary>
    public const string ToolName = "media.tool.name";

    /// <summary>Actor id that initiated the call (e.g. <c>agent-alpha</c>, <c>agent-alpha</c>). Low-cardinality.</summary>
    public const string ActorId = "media.actor.id";
}
