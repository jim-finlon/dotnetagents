using System.Text.Json.Serialization;

namespace DotNetAgents.ContextIntent;

/// <summary>
/// Where this ContextIntent came from. Captured at emission time and never edited; consumers
/// rely on provenance to audit the chain of intent across handoffs.
/// </summary>
public sealed record ProvenanceSpec
{
    [JsonPropertyName("actor")]
    public required ActorRef Actor { get; init; }

    [JsonPropertyName("origin")]
    public required ProvenanceOrigin Origin { get; init; }

    [JsonPropertyName("captured_at")]
    public required DateTimeOffset CapturedAt { get; init; }

    /// <summary>
    /// Labels of pattern classes scrubbed from input by transcript extractors before layer
    /// assembly. Empty when no scrubbing was needed; populated when secret-shaped substrings
    /// were redacted (e.g. "github_pat", "openai_key", "smtp_password").
    /// </summary>
    [JsonPropertyName("scrubbed_patterns")]
    public IReadOnlyList<string>? ScrubbedPatterns { get; init; }

    /// <summary>Optional reference to the upstream intent this one was derived from (when handing off).</summary>
    [JsonPropertyName("derived_from_task_id")]
    public string? DerivedFromTaskId { get; init; }
}

/// <summary>Actor identity. Use the SDLC actor enum.</summary>
public sealed record ActorRef(
    [property: JsonPropertyName("actor_type")] string ActorType,
    [property: JsonPropertyName("actor_id")] string ActorId,
    [property: JsonPropertyName("display_name")] string? DisplayName = null);

/// <summary>Where the intent originated. Match the JSON Schema enum exactly.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProvenanceOrigin
{
    [JsonStringEnumMemberName("voice_note")] VoiceNote,
    [JsonStringEnumMemberName("chat")] Chat,
    [JsonStringEnumMemberName("sdlc_story")] SdlcStory,
    [JsonStringEnumMemberName("operator_cli")] OperatorCli,
    [JsonStringEnumMemberName("agent_emission")] AgentEmission,
    [JsonStringEnumMemberName("scheduled_task")] ScheduledTask,
    [JsonStringEnumMemberName("other")] Other,
}
