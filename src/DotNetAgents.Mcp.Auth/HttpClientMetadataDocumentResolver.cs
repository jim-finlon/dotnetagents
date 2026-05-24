// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Default <see cref="IClientMetadataDocumentResolver"/> — fetches the CIMD over HTTPS using
/// <see cref="HttpClient"/>, validates it against <see cref="McpAuthOptions"/>, and caches
/// resolved documents for <see cref="McpAuthOptions.ClientMetadataCacheDuration"/>.
/// </summary>
public sealed class HttpClientMetadataDocumentResolver : IClientMetadataDocumentResolver
{
    private readonly HttpClient _httpClient;
    private readonly McpAuthOptions _options;
    private readonly ILogger<HttpClientMetadataDocumentResolver> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeProvider _clock;

    public HttpClientMetadataDocumentResolver(
        HttpClient httpClient,
        IOptions<McpAuthOptions> options,
        ILogger<HttpClientMetadataDocumentResolver>? logger = null,
        TimeProvider? clock = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<HttpClientMetadataDocumentResolver>.Instance;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ClientMetadataResolution> ResolveAsync(Uri clientIdUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientIdUrl);

        if (!string.Equals(clientIdUrl.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return new ClientMetadataResolution(null, new[] { "CIMD: client_id URL must use https scheme." }, false);
        }

        var key = clientIdUrl.AbsoluteUri;
        if (_cache.TryGetValue(key, out var cached) && cached.ExpiresAtUtc > _clock.GetUtcNow())
        {
            return new ClientMetadataResolution(cached.Document, cached.Errors, FromCache: true);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.ClientMetadataFetchTimeout);

        ClientMetadataDocument? document;
        IReadOnlyList<string> errors;
        try
        {
            using var response = await _httpClient.GetAsync(clientIdUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                errors = new[] { $"CIMD: fetch returned HTTP {(int)response.StatusCode}." };
                document = null;
            }
            else
            {
                if (response.Content.Headers.ContentLength is { } len && len > _options.MaxClientMetadataBytes)
                {
                    errors = new[] { $"CIMD: response Content-Length {len} exceeds limit {_options.MaxClientMetadataBytes}." };
                    document = null;
                }
                else
                {
                    document = await response.Content.ReadFromJsonAsync<ClientMetadataDocument>(cancellationToken: cts.Token).ConfigureAwait(false);
                    errors = ClientMetadataDocumentValidator.Validate(document, clientIdUrl, _options);
                    if (errors.Count > 0) document = null;
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            errors = new[] { "CIMD: fetch timed out." };
            document = null;
        }
        catch (HttpRequestException ex)
        {
            errors = new[] { $"CIMD: HTTP error during fetch: {ex.Message}" };
            document = null;
            _logger.LogDebug(ex, "CIMD fetch failed for {Url}", clientIdUrl);
        }
        catch (JsonException ex)
        {
            errors = new[] { $"CIMD: response is not valid JSON: {ex.Message}" };
            document = null;
        }

        var entry = new CacheEntry(document, errors, _clock.GetUtcNow().Add(_options.ClientMetadataCacheDuration));
        _cache[key] = entry;
        return new ClientMetadataResolution(document, errors, FromCache: false);
    }

    private sealed record CacheEntry(ClientMetadataDocument? Document, IReadOnlyList<string> Errors, DateTimeOffset ExpiresAtUtc);
}
