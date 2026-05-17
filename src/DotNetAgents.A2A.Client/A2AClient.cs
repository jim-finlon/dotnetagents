using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DotNetAgents.A2A;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.A2A.Client;

/// <summary>
/// Default <see cref="IA2AClient"/> implementation. Uses an injected <see cref="HttpClient"/>
/// (typically via <see cref="IHttpClientFactory"/>); caches Agent Cards in-memory by base URL
/// for the configured TTL.
/// </summary>
public sealed class A2AClient : IA2AClient
{
    private readonly HttpClient _http;
    private readonly ILogger<A2AClient>? _logger;
    private readonly TimeSpan _agentCardTtl;
    private readonly ConcurrentDictionary<string, CachedAgentCard> _cache = new();

    public A2AClient(
        HttpClient http,
        ILogger<A2AClient>? logger = null,
        TimeSpan? agentCardTtl = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger;
        _agentCardTtl = agentCardTtl ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    public async Task<AgentCard> DiscoverAsync(Uri baseUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        var key = baseUrl.GetLeftPart(UriPartial.Authority);

        if (_cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return cached.Card;
        }

        var url = new Uri(baseUrl, "/.well-known/agent.json");
        var card = await _http.GetFromJsonAsync<AgentCard>(url, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException($"Agent card at {url} returned null.");

        _cache[key] = new CachedAgentCard(card, DateTimeOffset.UtcNow.Add(_agentCardTtl));
        return card;
    }

    /// <inheritdoc />
    public async Task<A2AResponse> SendTaskAsync(
        Uri baseUrl,
        A2ATask task,
        A2AClientCallOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentNullException.ThrowIfNull(task);
        options ??= new A2AClientCallOptions();

        var url = BuildUrl(baseUrl, options.BaseRoute, "tasks/send");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(task) };
        ApplyAuth(request, options.BearerToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (options.Timeout is { } t) timeoutCts.CancelAfter(t);
        else timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeoutCts.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<A2AResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return body ?? throw new InvalidOperationException("A2A server returned a null response body.");
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<A2AEvent> StreamTaskAsync(
        Uri baseUrl,
        A2ATask task,
        A2AClientCallOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        ArgumentNullException.ThrowIfNull(task);
        options ??= new A2AClientCallOptions();

        var url = BuildUrl(baseUrl, options.BaseRoute, "tasks/sendSubscribe");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(task) };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        ApplyAuth(request, options.BearerToken);

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, leaveOpen: false);

        // Minimal SSE parser: lines starting with "data:" are payloads; blank line dispatches.
        var dataLines = new List<string>();
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;

            if (line.Length == 0)
            {
                if (dataLines.Count > 0)
                {
                    var json = string.Join("\n", dataLines);
                    dataLines.Clear();
                    A2AEvent? evt = null;
                    try { evt = JsonSerializer.Deserialize<A2AEvent>(json); }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "A2A SSE event payload parse failed; skipping. Payload: {Json}", json);
                    }
                    if (evt is not null) yield return evt;
                }
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                dataLines.Add(line.Length > 5 ? line[5..].TrimStart() : string.Empty);
            }
            // event: / id: / retry: lines are noted by SSE but our consumer only needs the JSON payload.
        }

        // Flush any trailing event without a final blank line.
        if (dataLines.Count > 0)
        {
            var json = string.Join("\n", dataLines);
            A2AEvent? evt = null;
            try { evt = JsonSerializer.Deserialize<A2AEvent>(json); }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "A2A SSE trailing event payload parse failed; skipping.");
            }
            if (evt is not null) yield return evt;
        }
    }

    private static Uri BuildUrl(Uri baseUrl, string baseRoute, string path)
    {
        var trimmedRoute = baseRoute.TrimStart('/').TrimEnd('/');
        var trimmedPath = path.TrimStart('/');
        var combined = $"/{trimmedRoute}/{trimmedPath}";
        return new Uri(baseUrl, combined);
    }

    private static void ApplyAuth(HttpRequestMessage request, string? bearerToken)
    {
        if (!string.IsNullOrEmpty(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }

    private sealed record CachedAgentCard(AgentCard Card, DateTimeOffset ExpiresAt);
}
