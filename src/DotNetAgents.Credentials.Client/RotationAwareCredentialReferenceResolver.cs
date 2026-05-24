// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using DotNetAgents.Abstractions.PublicSubstitutes.Credentials;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Credentials.Client;

/// <summary>
/// Rotation-aware credential resolver that keeps short-lived in-memory copies
/// and can refresh one reference after a downstream auth failure.
/// </summary>
public sealed class RotationAwareCredentialReferenceResolver : IRotationAwareCredentialAccessor, IDisposable
{
    private readonly ICredentialReferenceResolver _inner;
    private readonly RotationAwareCredentialAccessorOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<CredentialCacheKey, CacheEntry> _cache = new();

    public RotationAwareCredentialReferenceResolver(
        ICredentialReferenceResolver inner,
        IOptions<RotationAwareCredentialAccessorOptions>? options = null,
        TimeProvider? timeProvider = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _options = options?.Value ?? new RotationAwareCredentialAccessorOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (_options.CacheTtl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "CacheTtl must be greater than zero.");
        }
    }

    /// <inheritdoc />
    public ValueTask<ICredentialAccessor> ResolveAsync(
        CredentialReference reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();

        var key = CredentialCacheKey.From(reference);
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > _timeProvider.GetUtcNow())
        {
            return ValueTask.FromResult<ICredentialAccessor>(
                new CachedCredentialAccessor(reference, entry.CopyValue()));
        }

        return RefreshAsync(reference, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ICredentialAccessor> RefreshAsync(
        CredentialReference reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();

        var key = CredentialCacheKey.From(reference);
        var value = await FetchCopyAsync(reference, cancellationToken).ConfigureAwait(false);
        var expiresAt = _timeProvider.GetUtcNow().Add(_options.CacheTtl);
        var replacement = new CacheEntry(value, expiresAt);

        _cache.AddOrUpdate(
            key,
            replacement,
            (_, existing) =>
            {
                existing.Clear();
                return replacement;
            });

        return new CachedCredentialAccessor(reference, replacement.CopyValue());
    }

    /// <inheritdoc />
    public void Invalidate(CredentialReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var key = CredentialCacheKey.From(reference);
        if (_cache.TryRemove(key, out var entry))
        {
            entry.Clear();
        }
    }

    /// <inheritdoc />
    public ValueTask<ICredentialAccessor> RefreshAfterAuthenticationFailureAsync(
        CredentialReference reference,
        CancellationToken cancellationToken = default)
    {
        Invalidate(reference);
        return RefreshAsync(reference, cancellationToken);
    }

    public void Dispose()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Clear();
        }

        _cache.Clear();
    }

    private async ValueTask<char[]> FetchCopyAsync(CredentialReference reference, CancellationToken cancellationToken)
    {
        await using var accessor = await _inner.ResolveAsync(reference, cancellationToken).ConfigureAwait(false);
        var view = await accessor.AccessAsync(cancellationToken).ConfigureAwait(false);
        return view.Value.Span.ToArray();
    }

    private readonly record struct CredentialCacheKey(string Category, string Name, string Version)
    {
        public static CredentialCacheKey From(CredentialReference reference) =>
            new(reference.Category, reference.Name, reference.Version ?? string.Empty);
    }

    private sealed class CacheEntry
    {
        private char[]? _value;

        public CacheEntry(char[] value, DateTimeOffset expiresAtUtc)
        {
            _value = value;
            ExpiresAtUtc = expiresAtUtc;
        }

        public DateTimeOffset ExpiresAtUtc { get; }

        public char[] CopyValue()
        {
            ObjectDisposedException.ThrowIf(_value is null, this);
            return _value.AsSpan().ToArray();
        }

        public void Clear()
        {
            var value = Interlocked.Exchange(ref _value, null);
            if (value is not null)
            {
                Array.Clear(value);
            }
        }
    }

    private sealed class CachedCredentialAccessor : ICredentialAccessor
    {
        private char[]? _buffer;

        public CachedCredentialAccessor(CredentialReference reference, char[] buffer)
        {
            Reference = reference;
            _buffer = buffer;
        }

        public CredentialReference Reference { get; }

        public ValueTask<SecretView> AccessAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_buffer is null, this);
            return ValueTask.FromResult(new SecretView(_buffer.AsMemory()));
        }

        public ValueTask DisposeAsync()
        {
            var buffer = Interlocked.Exchange(ref _buffer, null);
            if (buffer is not null)
            {
                Array.Clear(buffer);
            }

            return ValueTask.CompletedTask;
        }
    }
}
