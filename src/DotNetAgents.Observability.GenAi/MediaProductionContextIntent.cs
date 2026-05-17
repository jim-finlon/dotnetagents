using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Observability.GenAi;

/// <summary>
/// Strongly-typed ContextIntent v1 payload (schema docs/schemas/context-intent.v1.json).
/// Story edf42bd1 (P7.5 T4 / FR-SP705). Media-production MCP/A2A tool emit one of these at
/// task boundaries; consumers serialize via <see cref="ToJsonString"/> for observability
/// pipelines.
/// </summary>
/// <remarks>
/// Required fields per the schema: task_id, intent, context_layers, provenance.
/// schema_version is pinned to "1.0" and serialized; secret material MUST be referenced
/// through <see cref="ContextLayer.CredentialRefs"/>, never inlined into
/// <see cref="ContextLayer.Content"/>.
/// </remarks>
public sealed record ContextIntentV1(
    [property: JsonPropertyName("task_id")] string TaskId,
    [property: JsonPropertyName("intent")] ContextIntentBody Intent,
    [property: JsonPropertyName("context_layers")] IReadOnlyList<ContextLayer> ContextLayers,
    [property: JsonPropertyName("provenance")] ContextProvenance Provenance,
    [property: JsonPropertyName("constraints")] IReadOnlyList<string>? Constraints = null,
    [property: JsonPropertyName("acceptance")] IReadOnlyList<string>? Acceptance = null,
    [property: JsonPropertyName("extensions"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, object>? Extensions = null)
{
    /// <summary>Pinned schema version (always "1.0" for v1 payloads).</summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "1.0";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Serialize this payload to a compact UTF-8 JSON string.</summary>
    public string ToJsonString() => JsonSerializer.Serialize(this, JsonOptions);
}

/// <summary>Inner <c>intent</c> object of the ContextIntent payload.</summary>
public sealed record ContextIntentBody(
    [property: JsonPropertyName("verb")] string Verb,
    [property: JsonPropertyName("goal")] string Goal,
    [property: JsonPropertyName("success_criteria"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? SuccessCriteria = null);

/// <summary>One context-layer slice attached to a ContextIntent payload.</summary>
public sealed record ContextLayer(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("content")] object Content,
    [property: JsonPropertyName("credential_refs"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<ContextCredentialRef>? CredentialRefs = null,
    [property: JsonPropertyName("truncated"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Truncated = false,
    [property: JsonPropertyName("captured_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? CapturedAt = null);

/// <summary>Reference to a CredentialsAgent category/name pair used by a context layer.</summary>
public sealed record ContextCredentialRef(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("name")] string Name);

/// <summary>Provenance metadata required by the v1 schema.</summary>
public sealed record ContextProvenance(
    [property: JsonPropertyName("actor")] ContextActor Actor,
    [property: JsonPropertyName("origin")] string Origin,
    [property: JsonPropertyName("captured_at")] DateTimeOffset CapturedAt,
    [property: JsonPropertyName("source_uri"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SourceUri = null,
    [property: JsonPropertyName("scrubbed_patterns"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? ScrubbedPatterns = null);

/// <summary>Actor identity in a provenance block.</summary>
public sealed record ContextActor(
    [property: JsonPropertyName("actor_type")] string ActorType,
    [property: JsonPropertyName("actor_id")] string ActorId,
    [property: JsonPropertyName("display_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DisplayName = null);

/// <summary>
/// Builder for media-production MCP/A2A tool-boundary ContextIntent payloads. Story edf42bd1
/// (P7.5 T4 / FR-SP705). Use <see cref="ForToolBoundary"/> as the canonical factory; it stamps
/// the conventional verb/origin and lets the caller add tool-specific layers.
/// </summary>
public static class MediaProductionContextIntent
{
    /// <summary>Build a tool-boundary ContextIntent v1 payload.</summary>
    /// <param name="toolName">MCP/A2A tool name (e.g. <c>generate_test_clip</c>, <c>estimate_cost</c>).</param>
    /// <param name="taskId">Stable task id (SDLC story id, storyboard id, or operator-assigned handle). Falls back to a tool-name-prefixed guid when unset.</param>
    /// <param name="actorType">Actor type per the v1 schema enum. Default: <c>AgentInstance</c>.</param>
    /// <param name="actorId">Stable actor id (e.g. <c>agent-alpha</c>).</param>
    /// <param name="goal">One-sentence goal statement.</param>
    /// <param name="extraLayers">Optional tool-specific layers; the manifest layer is always added.</param>
    public static ContextIntentV1 ForToolBoundary(
        string toolName,
        string? taskId,
        string actorType,
        string actorId,
        string goal,
        IEnumerable<ContextLayer>? extraLayers = null,
        IEnumerable<string>? constraints = null,
        IEnumerable<string>? acceptance = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(goal);

        var manifestLayer = new ContextLayer(
            Id: $"tool-manifest:{toolName}",
            Source: "tool_manifest",
            Scope: "turn",
            Content: new Dictionary<string, object>
            {
                ["service"] = MediaProductionMeter.Name,
                ["tool"] = toolName
            },
            CapturedAt: DateTimeOffset.UtcNow);

        var layers = new List<ContextLayer> { manifestLayer };
        if (extraLayers is not null) layers.AddRange(extraLayers);

        return new ContextIntentV1(
            TaskId: string.IsNullOrWhiteSpace(taskId) ? $"{toolName}:{Guid.NewGuid():N}" : taskId,
            Intent: new ContextIntentBody(Verb: "execute_tool", Goal: goal),
            ContextLayers: layers,
            Provenance: new ContextProvenance(
                Actor: new ContextActor(actorType, actorId),
                Origin: "agent_emission",
                CapturedAt: DateTimeOffset.UtcNow),
            Constraints: constraints?.ToList(),
            Acceptance: acceptance?.ToList());
    }
}
