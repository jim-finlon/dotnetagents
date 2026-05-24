// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DotNetAgents.Abstractions.PublicSubstitutes.Session;

namespace DotNetAgents.SessionPersistence.PublicSubstitutes.Session;

/// <summary>
/// Single-process local file adapter for public examples. It writes one JSON
/// file per session and caches reads in-process; it is not a distributed store.
/// </summary>
public sealed class FileSnapshotSessionStore : IPublicSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<SessionId, SessionSnapshot> _cache = new();
    private readonly string _directoryPath;

    public FileSnapshotSessionStore(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("A directory path is required.", nameof(directoryPath));
        }

        _directoryPath = directoryPath;
        Directory.CreateDirectory(_directoryPath);
    }

    public async ValueTask<SessionSnapshot?> LoadAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var path = PathFor(id);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        var snapshot = await JsonSerializer
            .DeserializeAsync<SessionSnapshot>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is not null)
        {
            _cache[snapshot.Id] = snapshot;
        }

        return snapshot;
    }

    public async ValueTask SaveAsync(
        SessionId id,
        SessionSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var normalized = snapshot with { Id = id };
        Directory.CreateDirectory(_directoryPath);

        var path = PathFor(id);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, path, overwrite: true);
        _cache[id] = normalized;
    }

    public ValueTask DeleteAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache.TryRemove(id, out _);
        var path = PathFor(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<SessionId> ListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directoryPath);
        foreach (var file in Directory.EnumerateFiles(_directoryPath, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = File.OpenRead(file);
            var snapshot = await JsonSerializer
                .DeserializeAsync<SessionSnapshot>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (snapshot is not null)
            {
                _cache[snapshot.Id] = snapshot;
                yield return snapshot.Id;
            }
        }
    }

    private string PathFor(SessionId id)
    {
        var escaped = Uri.EscapeDataString(id.Value);
        return Path.Combine(_directoryPath, $"{escaped}.json");
    }
}
