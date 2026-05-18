using System.Text.Json.Serialization;

namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Bootstrap payload for GET /mcp/instructions (DNA MCP consumer directory convention).
/// </summary>
public sealed class McpInstructionsResponse
{
    /// <summary>snake_case service name (e.g. repo_intelligence).</summary>
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>One paragraph: what this MCP service does.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Tells consumers to call GET /mcp/tools next, then POST /mcp/tools/call.</summary>
    [JsonPropertyName("bootstrapStep")]
    public string BootstrapStep { get; set; } = string.Empty;

    /// <summary>Clarifies base URL for this host.</summary>
    [JsonPropertyName("baseUrlNote")]
    public string? BaseUrlNote { get; set; }

    /// <summary>Who may use this service (LLMs, voice hosts, IDE agents, etc.).</summary>
    [JsonPropertyName("consumers")]
    public string? Consumers { get; set; }

    /// <summary>Link to DNA MCP consumer directory or repo path.</summary>
    [JsonPropertyName("directoryLink")]
    public string? DirectoryLink { get; set; }

    /// <summary>Optional per-client config hints (Cursor, Claude, Codex, etc.).</summary>
    [JsonPropertyName("configSnippets")]
    public IReadOnlyDictionary<string, string>? ConfigSnippets { get; set; }

    /// <summary>
    /// Optional prompt composition mode for services that build LLM instructions from a known
    /// prompt architecture. Valid values are <c>monolithic</c>, <c>routed</c>, and
    /// <c>primary_subordinate</c>. Omit when the service does not use model prompts or has not
    /// declared a composition contract yet.
    /// </summary>
    [JsonPropertyName("compositionMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CompositionMode { get; set; }

    /// <summary>
    /// Optional non-secret prompt fragment identifiers that participate in the declared
    /// <see cref="CompositionMode"/>. These are ids, versions, or stable refs only; never prompt
    /// bodies, user text, credentials, or provider secrets.
    /// </summary>
    [JsonPropertyName("promptFragmentIds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? PromptFragmentIds { get; set; }

    /// <summary>
    /// Optional human-readable note explaining how consumers should interpret the prompt
    /// composition metadata.
    /// </summary>
    [JsonPropertyName("promptCompositionNotes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PromptCompositionNotes { get; set; }

    /// <summary>
    /// Optional service-specific extras (e.g. auth, transports, recommendedDiscoveryOrder, dataModel, lessonCapture).
    /// Marked with [JsonExtensionData] so entries serialize flat at the top level of the JSON object —
    /// they appear as siblings of the declared properties, not nested under an "extensions" key. This
    /// keeps the wire shape identical to the pre-typed anonymous-object payloads services previously
    /// emitted. During deserialization, any JSON properties that do not match a declared member are
    /// collected here as JsonElement values.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object>? Extensions { get; set; }
}

/// <summary>
/// Standard <see cref="McpInstructionsResponse.CompositionMode"/> values.
/// </summary>
public static class McpPromptCompositionModes
{
    /// <summary>One stable instruction block; no route-specific fragments are selected.</summary>
    public const string Monolithic = "monolithic";

    /// <summary>A stable core plus keyed fragments selected by lane, task, or workflow phase.</summary>
    public const string Routed = "routed";

    /// <summary>A primary planner/supervisor prompt with subordinate worker prompt contracts.</summary>
    public const string PrimarySubordinate = "primary_subordinate";

    /// <summary>All standard composition modes.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Monolithic,
        Routed,
        PrimarySubordinate
    };
}
