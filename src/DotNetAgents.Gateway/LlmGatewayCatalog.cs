using System.Text.Json;

namespace DotNetAgents.Gateway;

/// <summary>
/// Typed projection of one model entry returned by an LLM gateway's <c>/models</c> response.
/// Matches the structured <c>catalog</c> sub-object the gateway emits per story 9cc2b249
/// (Local-LLM-First Routing and Telemetry Activation). The model fitness router consumes this
/// before considering external escalation.
/// </summary>
/// <param name="Id">Stable model id (e.g. <c>"qwen3-32b"</c>).</param>
/// <param name="DisplayName">Operator-readable label.</param>
/// <param name="Family">Family/provider grouping (e.g. <c>"qwen"</c>, <c>"llama"</c>, <c>"deepseek"</c>).</param>
/// <param name="Tier">Speed tier the operator declared (<c>"fast"</c>, <c>"standard"</c>, <c>"premium"</c>).</param>
/// <param name="ContextWindow">Max prompt+completion tokens the model supports. Null when the gateway didn't surface it.</param>
/// <param name="Modalities">Input modalities the model accepts (e.g. <c>["text"]</c> or <c>["text","vision"]</c>).</param>
/// <param name="ToolFunctionSupport">Whether the model supports function/tool calling.</param>
/// <param name="Quantization">Weight quantization label (e.g. <c>"4-bit"</c>).</param>
/// <param name="MemoryRequirements">Weight + estimated runtime memory the gateway expects this model to need.</param>
/// <param name="LoadTimeEstimateSeconds">Heuristic seconds-to-warm estimate, capped by the gateway's runtime ceiling.</param>
/// <param name="WarmStatus">Current state on the gateway: <c>warm</c>/<c>loading</c>/<c>cold</c>/<c>unavailable</c>.</param>
/// <param name="MaxConcurrency">Concurrent request slots the gateway allocates for this model.</param>
/// <param name="TaskDomainTags">Operator-declared task/domain tags this model is well-suited for.</param>
/// <param name="TemplateFamily">
/// Optional vLLM tool-parser template family token (<c>hermes</c>, <c>mistral</c>, <c>llama3_json</c>,
/// <c>deepseek_v3</c>, <c>xlam</c>, <c>pythonic</c>) the gateway reports for this model. Pairs with
/// <see cref="ToolFunctionSupport"/> so the local-LLM skill selector knows which projected artifact
/// to load (<c>tool.json</c> + <c>template-hint.json</c> when the family is known, otherwise
/// fallback to <c>prompt-fragment.md</c>). Null/empty means the gateway didn't declare a family
/// even though tool-call support may be on — selector should fall back to plain-prompt.
/// </param>
public sealed record LlmGatewayCatalogEntry(
    string Id,
    string? DisplayName,
    string Family,
    string Tier,
    int? ContextWindow,
    IReadOnlyList<string> Modalities,
    bool ToolFunctionSupport,
    string Quantization,
    LlmGatewayMemoryRequirements MemoryRequirements,
    int LoadTimeEstimateSeconds,
    LlmGatewayWarmStatus WarmStatus,
    int MaxConcurrency,
    IReadOnlyList<string> TaskDomainTags,
    string? TemplateFamily = null);

/// <summary>Weight + estimated runtime memory footprint per <see cref="LlmGatewayCatalogEntry"/>.</summary>
public sealed record LlmGatewayMemoryRequirements(double? WeightGb, double? EstimatedRuntimeGb);

/// <summary>Per-model warm/cold state as reported by the gateway's slot manager.</summary>
public enum LlmGatewayWarmStatus
{
    /// <summary>The gateway didn't report a known status. The fitness router should treat this as cold.</summary>
    Unknown = 0,

    /// <summary>Slot is loaded and ready to serve immediately.</summary>
    Warm = 1,

    /// <summary>Slot is currently loading or switching to this model.</summary>
    Loading = 2,

    /// <summary>Model is downloaded but no slot is currently holding it.</summary>
    Cold = 3,

    /// <summary>Model can't be loaded right now (not downloaded, errored, or blocked).</summary>
    Unavailable = 4,
}

/// <summary>
/// Parser for the JSON shape the Python <c>llm-host-gateway</c> emits at <c>GET /models</c> and
/// <c>GET /models/{id}</c>. Tolerates missing optional fields so older gateway versions that
/// haven't shipped story 9cc2b249 yet still parse cleanly into <see cref="LlmGatewayCatalogEntry"/>
/// (just with default/empty enrichment values).
/// </summary>
public static class LlmGatewayCatalogParser
{
    /// <summary>Parse a single-model response (<c>GET /models/{id}</c>).</summary>
    public static LlmGatewayCatalogEntry ParseSingle(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Single-model response JSON cannot be empty.", nameof(json));
        using var doc = JsonDocument.Parse(json);
        return ParseEntry(doc.RootElement);
    }

    /// <summary>Parse a list response (<c>GET /models</c>) — the wrapper has shape <c>{"models": [...]}</c>.</summary>
    public static IReadOnlyList<LlmGatewayCatalogEntry> ParseList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("List response JSON cannot be empty.", nameof(json));
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("models", out var modelsArray) || modelsArray.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Gateway list response must contain a 'models' array.");
        }
        var entries = new List<LlmGatewayCatalogEntry>(modelsArray.GetArrayLength());
        foreach (var element in modelsArray.EnumerateArray())
        {
            entries.Add(ParseEntry(element));
        }
        return entries;
    }

    private static LlmGatewayCatalogEntry ParseEntry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new JsonException("Catalog entry must be a JSON object.");

        var id = RequireString(element, "id");
        var displayName = OptionalString(element, "display_name");
        var catalog = OptionalObject(element, "catalog");

        // When the catalog sub-object is absent (older gateway), fall back to safe defaults so the
        // consumer can still surface the model's existence even though enrichment is missing.
        if (catalog is null)
        {
            return new LlmGatewayCatalogEntry(
                Id: id,
                DisplayName: displayName,
                Family: "unknown",
                Tier: OptionalString(element, "speed_tier") ?? "standard",
                ContextWindow: OptionalInt(element, "max_context_length"),
                Modalities: new[] { "text" },
                ToolFunctionSupport: false,
                Quantization: OptionalString(element, "quant") ?? "unknown",
                MemoryRequirements: new LlmGatewayMemoryRequirements(OptionalDouble(element, "weight_size_gb"), null),
                LoadTimeEstimateSeconds: 0,
                WarmStatus: LlmGatewayWarmStatus.Unknown,
                MaxConcurrency: 1,
                TaskDomainTags: ReadStringArray(element, "task_categories"),
                TemplateFamily: null);
        }

        var catalogElement = catalog.Value;
        return new LlmGatewayCatalogEntry(
            Id: id,
            DisplayName: displayName,
            Family: OptionalString(catalogElement, "family") ?? "unknown",
            Tier: OptionalString(catalogElement, "tier") ?? "standard",
            ContextWindow: OptionalInt(catalogElement, "context_window"),
            Modalities: ReadStringArray(catalogElement, "modalities", fallback: new[] { "text" }),
            ToolFunctionSupport: OptionalBool(catalogElement, "tool_function_support") ?? false,
            Quantization: OptionalString(catalogElement, "quantization") ?? "unknown",
            MemoryRequirements: ParseMemoryRequirements(catalogElement),
            LoadTimeEstimateSeconds: OptionalInt(catalogElement, "load_time_estimate_seconds") ?? 0,
            WarmStatus: ParseWarmStatus(OptionalString(catalogElement, "warm_status")),
            MaxConcurrency: OptionalInt(catalogElement, "max_concurrency") ?? 1,
            TaskDomainTags: ReadStringArray(catalogElement, "task_domain_tags"),
            TemplateFamily: OptionalString(catalogElement, "template_family"));
    }

    private static LlmGatewayMemoryRequirements ParseMemoryRequirements(JsonElement catalog)
    {
        if (!catalog.TryGetProperty("memory_requirements", out var mem) || mem.ValueKind != JsonValueKind.Object)
        {
            return new LlmGatewayMemoryRequirements(null, null);
        }
        return new LlmGatewayMemoryRequirements(
            WeightGb: OptionalDouble(mem, "weight_gb"),
            EstimatedRuntimeGb: OptionalDouble(mem, "estimated_runtime_gb"));
    }

    private static LlmGatewayWarmStatus ParseWarmStatus(string? raw) =>
        raw?.ToLowerInvariant() switch
        {
            "warm" => LlmGatewayWarmStatus.Warm,
            "loading" => LlmGatewayWarmStatus.Loading,
            "cold" => LlmGatewayWarmStatus.Cold,
            "unavailable" => LlmGatewayWarmStatus.Unavailable,
            _ => LlmGatewayWarmStatus.Unknown,
        };

    private static string RequireString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var v) || v.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Catalog entry missing required string '{property}'.");
        }
        return v.GetString()!;
    }

    private static string? OptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? OptionalInt(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number when v.TryGetInt32(out var i) => i,
            JsonValueKind.Number => (int)v.GetDouble(),
            _ => null,
        };
    }

    private static double? OptionalDouble(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var v)) return null;
        return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
    }

    private static bool? OptionalBool(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static JsonElement? OptionalObject(JsonElement element, string property) =>
        element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Object ? v : null;

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string property, IReadOnlyList<string>? fallback = null)
    {
        if (!element.TryGetProperty(property, out var v) || v.ValueKind != JsonValueKind.Array)
        {
            return fallback ?? Array.Empty<string>();
        }
        var list = new List<string>(v.GetArrayLength());
        foreach (var item in v.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String) list.Add(item.GetString()!);
        }
        return list;
    }
}

/// <summary>
/// Client for the LLM gateway's <c>/models</c> endpoints. Hides the HTTP detail behind a typed
/// surface the model fitness router (a future story) consumes.
/// </summary>
public interface ILlmGatewayCatalogClient
{
    /// <summary>List the gateway's full model catalog.</summary>
    Task<IReadOnlyList<LlmGatewayCatalogEntry>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Look up a single model by id. Returns null when the gateway returns 404.</summary>
    Task<LlmGatewayCatalogEntry?> GetAsync(string modelId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="ILlmGatewayCatalogClient"/> backed by an injected <see cref="HttpClient"/>.
/// The HttpClient is expected to have <c>BaseAddress</c> set to the gateway root and any auth
/// headers (e.g. <c>X-Gateway-Token</c>) pre-configured by the DI registration.
/// </summary>
public sealed class HttpLlmGatewayCatalogClient : ILlmGatewayCatalogClient
{
    private readonly HttpClient _http;

    public HttpLlmGatewayCatalogClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<IReadOnlyList<LlmGatewayCatalogEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        var json = await _http.GetStringAsync("/models", cancellationToken).ConfigureAwait(false);
        return LlmGatewayCatalogParser.ParseList(json);
    }

    public async Task<LlmGatewayCatalogEntry?> GetAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("modelId cannot be empty.", nameof(modelId));
        using var response = await _http.GetAsync($"/models/{Uri.EscapeDataString(modelId)}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return LlmGatewayCatalogParser.ParseSingle(json);
    }
}
