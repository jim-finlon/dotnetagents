using System.Collections.Concurrent;
using DotNetAgents.Abstractions.Models;

namespace DotNetAgents.Memory.Advanced;

/// <summary>
/// In-memory episodic store: similarity via embedding, temporal via time range. FR-MEM-001.
/// </summary>
public sealed class InMemoryEpisodicMemory : IEpisodicMemory
{
    private readonly IEmbeddingModel _embeddings;
    private readonly ConcurrentDictionary<string, Episode> _byId = new();
    private readonly List<Episode> _byTime = new();
    private readonly object _timeLock = new();

    public InMemoryEpisodicMemory(IEmbeddingModel embeddings)
    {
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
    }

    /// <inheritdoc />
    public async Task StoreEpisodeAsync(Episode episode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(episode);
        var textToEmbed = string.IsNullOrEmpty(episode.Description) ? episode.Context : $"{episode.Description} {episode.Context}";
        var embedding = await _embeddings.EmbedAsync(textToEmbed, cancellationToken).ConfigureAwait(false);
        var withEmbedding = episode with { Embedding = embedding };
        _byId[episode.Id] = withEmbedding;
        lock (_timeLock)
        {
            _byTime.Add(withEmbedding);
            _byTime.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Episode>> RecallSimilarAsync(string cue, int k = 5, CancellationToken cancellationToken = default)
    {
        var queryVector = await _embeddings.EmbedAsync(cue, cancellationToken).ConfigureAwait(false);
        Episode[] list;
        lock (_timeLock)
        {
            list = _byTime.ToArray();
        }
        if (list.Length == 0) return Array.Empty<Episode>();
        var withScores = list
            .Where(e => e.Embedding != null && e.Embedding.Length == queryVector.Length)
            .Select(e => (Episode: e, Score: CosineSimilarity(queryVector, e.Embedding!)))
            .OrderByDescending(x => x.Score)
            .Take(k)
            .Select(x => x.Episode)
            .ToList();
        return withScores;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Episode>> RecallTemporalAsync(DateTimeOffset start, DateTimeOffset endTime, int limit = 50, CancellationToken cancellationToken = default)
    {
        lock (_timeLock)
        {
            var inRange = _byTime
                .Where(e => e.Timestamp >= start && e.Timestamp <= endTime)
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<Episode>>(inRange);
        }
    }

    /// <inheritdoc />
    public Task ConsolidateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0f;
        float dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        var denom = MathF.Sqrt(na) * MathF.Sqrt(nb);
        return denom > 0 ? dot / denom : 0f;
    }
}
