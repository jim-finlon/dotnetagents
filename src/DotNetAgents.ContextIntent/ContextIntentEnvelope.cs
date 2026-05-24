// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace DotNetAgents.ContextIntent;

/// <summary>
/// Root payload of the DNA Context Intent v1 contract. Matches
/// <c>docs/schemas/context-intent.v1.json</c>.
/// </summary>
/// <remarks>
/// One ContextIntent payload travels with a unit of work across agent / runtime / boundary
/// transitions: voice-note → workflow orchestrator story, workflow orchestrator story → PromptSpecialist prompt
/// selection, JARVIS intent → MCP tool dispatch. The payload captures *intent* (verb + goal +
/// success criteria), *context layers* (composable slices of what the receiver needs to know),
/// *constraints* (lane/policy/security bounds), *acceptance* (executable-ish "done" signals),
/// and *provenance* (who produced this and when).
/// <para>
/// Schema version is pinned to <see cref="V1SchemaVersion"/>; v2 lives in a different file.
/// Secret material is NEVER inlined — use <see cref="ContextLayer.CredentialRefs"/> to point
/// at credential resolver entries that the consumer resolves at use time.
/// </para>
/// </remarks>
public sealed record ContextIntentEnvelope
{
    /// <summary>The pinned schema version literal for v1.</summary>
    public const string V1SchemaVersion = "1.0";

    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = V1SchemaVersion;

    [JsonPropertyName("task_id")]
    public required string TaskId { get; init; }

    [JsonPropertyName("intent")]
    public required IntentSpec Intent { get; init; }

    [JsonPropertyName("context_layers")]
    public required IReadOnlyList<ContextLayer> ContextLayers { get; init; }

    [JsonPropertyName("constraints")]
    public IReadOnlyList<string>? Constraints { get; init; }

    [JsonPropertyName("acceptance")]
    public IReadOnlyList<string>? Acceptance { get; init; }

    [JsonPropertyName("provenance")]
    public required ProvenanceSpec Provenance { get; init; }

    [JsonPropertyName("extensions")]
    public IReadOnlyDictionary<string, object>? Extensions { get; init; }
}
