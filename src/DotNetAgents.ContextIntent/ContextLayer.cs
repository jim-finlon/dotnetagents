// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace DotNetAgents.ContextIntent;

/// <summary>
/// One composable slice of context informing the task. Layers stack — global rules, then
/// project-scoped, then story-scoped, etc. Per-layer scope drives retrieval narrowing.
/// </summary>
public sealed record ContextLayer
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Provenance label, typically <c>hive_mind</c>, <c>session_persistence</c>, <c>project_rules</c>, <c>tool_manifest</c>, <c>history</c>, or an agent-specific label.</summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>How broadly this layer should apply. Retrieval narrows by scope.</summary>
    [JsonPropertyName("scope")]
    public required ContextLayerScope Scope { get; init; }

    /// <summary>Free-form layer payload. Secret material MUST be a credential resolver reference, not a value.</summary>
    [JsonPropertyName("content")]
    public required object Content { get; init; }

    /// <summary>Optional credential references for secrets the receiver needs to resolve at use time.</summary>
    [JsonPropertyName("credential_refs")]
    public IReadOnlyList<CredentialRef>? CredentialRefs { get; init; }

    /// <summary>Optional metadata the layer producer attached for debug/diagnostics.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>Scope buckets that layer retrieval narrows by. Match the JSON Schema enum exactly.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContextLayerScope
{
    [JsonStringEnumMemberName("global")] Global,
    [JsonStringEnumMemberName("project")] Project,
    [JsonStringEnumMemberName("epic")] Epic,
    [JsonStringEnumMemberName("story")] Story,
    [JsonStringEnumMemberName("session")] Session,
    [JsonStringEnumMemberName("turn")] Turn,
}

/// <summary>Reference to a credential resolver entry. Never inline secret material — keep it as a category/name pointer.</summary>
public sealed record CredentialRef(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("name")] string Name);
