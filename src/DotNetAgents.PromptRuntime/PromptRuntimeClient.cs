// SPDX-License-Identifier: Apache-2.0

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetAgents.PromptRuntime;

public sealed class PromptRuntimeClient : IPromptRuntimeClient
{
    private const string HttpClientName = "DotNetAgents.PromptRuntime";

    private static readonly Regex PlaceholderRegex = new(
        @"\{\{\s*(?<name>[A-Za-z][A-Za-z0-9_\-]*)\s*\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly IPromptRegistry _registry;
    private readonly IOptionsMonitor<PromptRuntimeOptions> _options;
    private readonly ILogger<PromptRuntimeClient> _log;
    private readonly TimeProvider _time;
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly object _cacheLock = new();

    public PromptRuntimeClient(
        IHttpClientFactory httpFactory,
        IPromptRegistry registry,
        IOptionsMonitor<PromptRuntimeOptions> options,
        ILogger<PromptRuntimeClient> log,
        TimeProvider? time = null)
    {
        _httpFactory = httpFactory;
        _registry = registry;
        _options = options;
        _log = log;
        _time = time ?? TimeProvider.System;
    }

    public async Task<PromptResult> ResolveAsync(PromptRequest request, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Key))
            throw new ArgumentException("request.Key is required.", nameof(request));

        var opts = _options.CurrentValue;
        var cached = TryReadCache(request.Key, opts.CacheTtl);
        if (cached is not null)
            return Substitute(cached.Result with { Source = PromptResultSource.Cached }, request);

        if (opts.LocalOnly || string.IsNullOrWhiteSpace(opts.BaseUrl))
            return Fallback(request, "LocalOnly or empty BaseUrl");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(request.Timeout ?? opts.Timeout);
            using var http = _httpFactory.CreateClient(HttpClientName);
            http.BaseAddress = new Uri(opts.BaseUrl);
            if (!string.IsNullOrWhiteSpace(opts.ApiKey))
                http.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", opts.ApiKey);

            var url = $"/tools/get_prompt?key={Uri.EscapeDataString(request.Key)}&includeMetadata={(request.IncludeMetadata ? "true" : "false")}";
            if (!string.IsNullOrWhiteSpace(request.AllocationKey))
                url += $"&allocationKey={Uri.EscapeDataString(request.AllocationKey)}";

            var response = await http.GetAsync(url, cts.Token).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Fallback(request, "PromptSpecialist returned 404");
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<PromptSpecialistEnvelope>(SerializerOptions, cts.Token).ConfigureAwait(false);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Text))
                return Fallback(request, "Empty PromptSpecialist payload");
            var registration = _registry.TryGet(request.Key);

            var remote = new PromptResult(
                Key: request.Key,
                Text: payload.Text,
                VariantId: payload.VariantId,
                Version: payload.Version,
                Source: PromptResultSource.RemoteLibrary,
                FitnessScore: payload.FitnessScore,
                UnresolvedPlaceholders: null,
                InstructionBinding: BuildInstructionBinding(registration));
            WriteCache(request.Key, remote, _time.GetUtcNow());
            return Substitute(remote, request, registration);
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException or IOException)
        {
            _log.LogWarning(ex, "PromptSpecialist unreachable for key {Key}; falling back to local registry.", request.Key);
            return Fallback(request, ex.GetType().Name);
        }
    }

    public async Task ReportOutcomeAsync(PromptOutcomeReport report, CancellationToken ct = default)
    {
        if (report is null) return;
        var opts = _options.CurrentValue;
        if (opts.DisableOutcomeReports || opts.LocalOnly || string.IsNullOrWhiteSpace(opts.BaseUrl))
            return;
        if (report.VariantId is null)
            return; // local-fallback served — no variant to tag.
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(opts.Timeout);
            using var http = _httpFactory.CreateClient(HttpClientName);
            http.BaseAddress = new Uri(opts.BaseUrl);
            if (!string.IsNullOrWhiteSpace(opts.ApiKey))
                http.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", opts.ApiKey);

            await http.PostAsJsonAsync(
                "/tools/report_outcome",
                new
                {
                    key = report.Key,
                    variantId = report.VariantId,
                    success = report.Success,
                    qualityScore = report.QualityScore,
                    taskId = report.TaskId,
                    idempotencyKey = report.IdempotencyKey
                },
                cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException or IOException)
        {
            _log.LogDebug(ex, "Failed to post outcome report for {Key}; best-effort path — ignoring.", report.Key);
        }
    }

    // --- helpers ---

    private PromptResult Fallback(PromptRequest request, string reasonTag)
    {
        var reg = _registry.TryGet(request.Key);
        if (reg is null)
            throw new PromptRegistrationMissingException(request.Key);
        _log.LogDebug("Using fallback prompt body for {Key} (reason={Reason}).", request.Key, reasonTag);
        var result = new PromptResult(
            Key: request.Key,
            Text: reg.FallbackBody,
            VariantId: null,
            Version: null,
            Source: PromptResultSource.LocalFallback,
            InstructionBinding: BuildInstructionBinding(reg));
        return Substitute(result, request, reg);
    }

    private static PromptResult Substitute(PromptResult seed, PromptRequest request, PromptRegistration? reg = null)
    {
        if (string.IsNullOrEmpty(seed.Text))
            return seed;

        var supplied = request.Variables ?? new Dictionary<string, string>(0);
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        var rendered = PlaceholderRegex.Replace(seed.Text, match =>
        {
            var name = match.Groups["name"].Value;
            if (supplied.TryGetValue(name, out var v))
                return v;
            if (reg?.FallbackVariables is not null && reg.FallbackVariables.TryGetValue(name, out var d))
                return d;
            unresolved.Add(name);
            return match.Value;
        });

        return seed with
        {
            Text = rendered,
            UnresolvedPlaceholders = unresolved.Count == 0 ? null : unresolved.ToArray()
        };
    }

    private CacheEntry? TryReadCache(string key, TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero)
            return null;
        lock (_cacheLock)
        {
            if (!_cache.TryGetValue(key, out var entry))
                return null;
            if (_time.GetUtcNow() - entry.StoredAt > ttl)
            {
                _cache.Remove(key);
                return null;
            }
            return entry;
        }
    }

    private void WriteCache(string key, PromptResult result, DateTimeOffset now)
    {
        lock (_cacheLock)
        {
            _cache[key] = new CacheEntry(result, now);
        }
    }

    private static PromptInstructionBinding? BuildInstructionBinding(PromptRegistration? registration)
    {
        if (registration is null)
            return null;

        var chainRefs = registration.ChainContractRefs ?? Array.Empty<string>();
        var skillRefs = registration.SkillRefs ?? Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(registration.InstructionArtifactRef) && chainRefs.Count == 0 && skillRefs.Count == 0)
            return null;

        return new PromptInstructionBinding(
            registration.InstructionArtifactRef,
            chainRefs,
            skillRefs);
    }

    private sealed record CacheEntry(PromptResult Result, DateTimeOffset StoredAt);

    private sealed class PromptSpecialistEnvelope
    {
        public string Text { get; set; } = string.Empty;
        public Guid? VariantId { get; set; }
        public int? Version { get; set; }
        public double? FitnessScore { get; set; }
    }
}

public sealed class PromptRegistrationMissingException : Exception
{
    public string Key { get; }
    public PromptRegistrationMissingException(string key)
        : base($"Prompt '{key}' was not found remotely and is not declared in the local IPromptRegistry. Register a PromptRegistration with a FallbackBody or configure PromptSpecialist before calling the runtime client.")
    {
        Key = key;
    }
}
