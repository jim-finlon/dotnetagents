// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Restart-surviving <see cref="IPkceConsentStore"/> backed by a local JSON
/// file. It is intended for single-node Core 4 services and keeps revoked
/// rows in the file for operator audit while default list/find calls expose
/// only active consent.
/// </summary>
public sealed class DurableFilePkceConsentStore : IPkceConsentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DurableFilePkceConsentStore(
        IOptions<DurableFilePkceConsentStoreOptions> options,
        TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _filePath = ResolveFilePath(options.Value.FilePath);
        _clock = clock ?? TimeProvider.System;
    }

    public async Task RecordAsync(PkceConsentRecord decision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (string.IsNullOrWhiteSpace(decision.ActorId))
            throw new ArgumentException("ActorId is required.", nameof(decision));
        if (string.IsNullOrWhiteSpace(decision.ClientId))
            throw new ArgumentException("ClientId is required.", nameof(decision));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadAsync(cancellationToken).ConfigureAwait(false);
            var normalized = Normalize(decision);
            var now = _clock.GetUtcNow();

            for (var i = 0; i < state.Consents.Count; i++)
            {
                var existing = state.Consents[i];
                if (existing.RevokedAtUtc is null && IsSameConsentSubject(existing, normalized))
                {
                    state.Consents[i] = existing with { RevokedAtUtc = now };
                }
            }

            state.Consents.Add(normalized);
            await SaveAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PkceConsentRecord?> FindCoveringAsync(
        string actorId,
        string clientId,
        IReadOnlyCollection<string> requestedScopes,
        CancellationToken cancellationToken = default,
        string? serviceName = null)
    {
        if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(clientId))
            return null;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadAsync(cancellationToken).ConfigureAwait(false);
            var now = _clock.GetUtcNow();
            var normalizedService = NormalizeServiceName(serviceName);

            return state.Consents
                .Where(record => string.Equals(record.ActorId, actorId, StringComparison.Ordinal)
                    && string.Equals(record.ClientId, clientId, StringComparison.Ordinal)
                    && string.Equals(NormalizeServiceName(record.ServiceName), normalizedService, StringComparison.Ordinal)
                    && record.RevokedAtUtc is null
                    && (record.ExpiresAtUtc is null || record.ExpiresAtUtc.Value > now)
                    && record.Decision == PkceConsentDecision.Allow
                    && (requestedScopes.Count == 0 || requestedScopes.All(scope => record.Scopes.Contains(scope, StringComparer.Ordinal))))
                .OrderByDescending(record => record.GrantedAtUtc)
                .FirstOrDefault();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<PkceConsentRecord>> ListAsync(
        string? actorIdFilter = null,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadAsync(cancellationToken).ConfigureAwait(false);
            var now = _clock.GetUtcNow();
            IEnumerable<PkceConsentRecord> records = state.Consents
                .Where(record => record.ExpiresAtUtc is null || record.ExpiresAtUtc.Value > now);

            if (!includeRevoked)
            {
                records = records.Where(record => record.RevokedAtUtc is null);
            }

            if (!string.IsNullOrWhiteSpace(actorIdFilter))
            {
                records = records.Where(record => string.Equals(record.ActorId, actorIdFilter, StringComparison.Ordinal));
            }

            return records
                .OrderByDescending(record => record.GrantedAtUtc)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RevokeAsync(Guid consentId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadAsync(cancellationToken).ConfigureAwait(false);
            var now = _clock.GetUtcNow();
            for (var i = 0; i < state.Consents.Count; i++)
            {
                if (state.Consents[i].Id == consentId && state.Consents[i].RevokedAtUtc is null)
                {
                    state.Consents[i] = state.Consents[i] with { RevokedAtUtc = now };
                    await SaveAsync(state, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<DurableFilePkceConsentState> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new DurableFilePkceConsentState();
        }

        await using var stream = File.OpenRead(_filePath);
        var state = await JsonSerializer.DeserializeAsync<DurableFilePkceConsentState>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return state ?? new DurableFilePkceConsentState();
    }

    private async Task SaveAsync(DurableFilePkceConsentState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }

    private static string ResolveFilePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = AppContext.BaseDirectory;
        }

        return Path.Combine(root, "dna", "mcp", "oauth-consents.json");
    }

    private static PkceConsentRecord Normalize(PkceConsentRecord record) =>
        record with { ServiceName = NormalizeServiceName(record.ServiceName) };

    private static string NormalizeServiceName(string? serviceName) =>
        string.IsNullOrWhiteSpace(serviceName) ? "DNA MCP" : serviceName.Trim();

    private static bool IsSameConsentSubject(PkceConsentRecord left, PkceConsentRecord right) =>
        string.Equals(left.ActorId, right.ActorId, StringComparison.Ordinal)
        && string.Equals(left.ClientId, right.ClientId, StringComparison.Ordinal)
        && string.Equals(NormalizeServiceName(left.ServiceName), NormalizeServiceName(right.ServiceName), StringComparison.Ordinal);

    private sealed class DurableFilePkceConsentState
    {
        public int SchemaVersion { get; set; } = 1;
        public List<PkceConsentRecord> Consents { get; set; } = new();
    }
}

public sealed class DurableFilePkceConsentStoreOptions
{
    public string? FilePath { get; set; }
}
