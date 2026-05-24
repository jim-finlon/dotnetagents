// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.RegularExpressions;
using DotNetAgents.Abstractions.Retrieval;

namespace DotNetAgents.Core.Retrieval;

public sealed class CanonicalHybridSearchService : ICanonicalHybridSearchService
{
    private static readonly Regex TokenPattern = new("[a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IReadOnlyDictionary<string, IndexedChunk> _chunksById;
    private readonly IReadOnlyList<CanonicalSourceSupport> _supportedSourceClasses;
    private readonly IVectorStore? _vectorStore;
    private readonly DateTimeOffset _refreshedAtUtc;

    public CanonicalHybridSearchService(
        IEnumerable<RetrievalReadyDocument> documents,
        IVectorStore? vectorStore = null,
        DateTimeOffset? refreshedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(documents);

        _vectorStore = vectorStore;
        _refreshedAtUtc = refreshedAtUtc ?? DateTimeOffset.UtcNow;
        _supportedSourceClasses = new CanonicalIngestionService().GetSupportedSourceTypes();
        _chunksById = documents
            .SelectMany(document => document.Chunks.Select(chunk => new IndexedChunk(document, chunk)))
            .ToDictionary(chunk => chunk.Chunk.ChunkId, StringComparer.OrdinalIgnoreCase);
    }

    public CanonicalHybridSearchIndexSummary GetIndexSummary()
        => new(
            _refreshedAtUtc,
            _chunksById.Values.Select(static chunk => chunk.Document.DocumentId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            _chunksById.Count,
            _supportedSourceClasses);

    public async Task<CanonicalHybridSearchResponse> SearchAsync(
        CanonicalHybridSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.QueryText);

        var indexedChunks = _chunksById.Values
            .Where(chunk => IsVisible(chunk, query))
            .ToArray();

        var lexicalScores = (query.Mode == HybridSearchMode.Semantic)
            ? new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            : BuildLexicalScores(query.QueryText, indexedChunks);

        Dictionary<string, SemanticMatch> semanticMatches = new(StringComparer.OrdinalIgnoreCase);
        if (query.Mode != HybridSearchMode.Lexical && query.QueryVector is { Length: > 0 })
        {
            if (_vectorStore is null)
                throw new InvalidOperationException("Semantic or hybrid search requires an IVectorStore.");

            semanticMatches = await BuildSemanticMatchesAsync(query, cancellationToken).ConfigureAwait(false);
        }

        var modeUsed = ResolveMode(query, semanticMatches.Count > 0);
        var hits = indexedChunks
            .Select(chunk =>
            {
                lexicalScores.TryGetValue(chunk.Chunk.ChunkId, out var lexicalScore);
                semanticMatches.TryGetValue(chunk.Chunk.ChunkId, out var semanticMatch);
                var semanticScore = semanticMatch?.Score ?? 0f;
                var combinedScore = modeUsed switch
                {
                    HybridSearchMode.Lexical => lexicalScore,
                    HybridSearchMode.Semantic => semanticScore,
                    _ => (lexicalScore * 0.45f) + (semanticScore * 0.55f)
                };

                return new
                {
                    Chunk = chunk,
                    LexicalScore = lexicalScore,
                    SemanticScore = semanticScore,
                    CombinedScore = combinedScore,
                    SemanticMetadata = semanticMatch?.Metadata
                };
            })
            .Where(hit => hit.CombinedScore > 0f)
            .OrderByDescending(hit => hit.CombinedScore)
            .ThenByDescending(hit => hit.SemanticScore)
            .ThenByDescending(hit => hit.LexicalScore)
            .Take(Math.Max(1, query.TopK))
            .Select((hit, index) => ToHit(hit.Chunk, index + 1, hit.LexicalScore, hit.SemanticScore, hit.CombinedScore, hit.SemanticMetadata))
            .ToArray();

        return new CanonicalHybridSearchResponse(modeUsed, GetIndexSummary(), hits);
    }

    private async Task<Dictionary<string, SemanticMatch>> BuildSemanticMatchesAsync(
        CanonicalHybridSearchQuery query,
        CancellationToken cancellationToken)
    {
        var results = await _vectorStore!.SearchAsync(
                query.QueryVector!,
                topK: Math.Max(query.TopK * 4, 20),
                filter: query.SemanticFilter is null ? null : new Dictionary<string, object>(query.SemanticFilter),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return results
            .Where(result => _chunksById.ContainsKey(result.Id))
            .GroupBy(result => result.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new SemanticMatch(
                    group.Max(static result => result.Score),
                    ToReadOnlyMetadata(group.OrderByDescending(static result => result.Score).First().Metadata)),
                StringComparer.OrdinalIgnoreCase);
    }

    private static HybridSearchMode ResolveMode(CanonicalHybridSearchQuery query, bool hasSemanticMatches)
        => query.Mode switch
        {
            HybridSearchMode.Hybrid when !hasSemanticMatches => HybridSearchMode.Lexical,
            _ => query.Mode
        };

    private static Dictionary<string, float> BuildLexicalScores(
        string queryText,
        IReadOnlyCollection<IndexedChunk> chunks)
    {
        var queryTokens = Tokenize(queryText);
        if (queryTokens.Count == 0)
            return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        var scores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in chunks)
        {
            var chunkTokens = Tokenize(chunk.Chunk.Text);
            if (chunkTokens.Count == 0)
                continue;

            var matchedCount = queryTokens.Count(token => chunkTokens.Contains(token));
            if (matchedCount == 0)
                continue;

            var score = matchedCount / (float)queryTokens.Count;
            if (chunk.Chunk.Text.Contains(queryText, StringComparison.OrdinalIgnoreCase))
                score += 0.25f;
            if (chunk.Document.Title.Contains(queryText, StringComparison.OrdinalIgnoreCase))
                score += 0.15f;

            scores[chunk.Chunk.ChunkId] = MathF.Min(1f, score);
        }

        return scores;
    }

    private static HashSet<string> Tokenize(string text)
        => TokenPattern.Matches(text)
            .Select(static match => match.Value.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool IsVisible(IndexedChunk chunk, CanonicalHybridSearchQuery query)
    {
        if (query.AllowedSourceClasses is { Count: > 0 } &&
            !query.AllowedSourceClasses.Contains(chunk.Document.SourceClass, StringComparer.OrdinalIgnoreCase))
            return false;

        if (query.AllowedSourceIds is { Count: > 0 } &&
            !query.AllowedSourceIds.Contains(chunk.Document.SourceId, StringComparer.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static CanonicalHybridSearchHit ToHit(
        IndexedChunk chunk,
        int rank,
        float lexicalScore,
        float semanticScore,
        float combinedScore,
        IReadOnlyDictionary<string, object>? semanticMetadata)
    {
        var explanations = new List<string>();
        if (lexicalScore > 0f)
            explanations.Add($"lexical:{lexicalScore:F2}");
        if (semanticScore > 0f)
            explanations.Add($"semantic:{semanticScore:F2}");
        if (chunk.Document.Title.Contains(chunk.Document.SourceId, StringComparison.OrdinalIgnoreCase))
            explanations.Add("title-derived-from-source");

        return new CanonicalHybridSearchHit(
            chunk.Document.DocumentId,
            chunk.Chunk.ChunkId,
            chunk.Document.SourceClass,
            chunk.Document.SourceId,
            chunk.Document.Title,
            BuildSnippet(chunk.Chunk.Text),
            rank,
            lexicalScore,
            semanticScore,
            combinedScore,
            explanations,
            chunk.Document.Lineage,
            semanticMetadata);
    }

    private static IReadOnlyDictionary<string, object>? ToReadOnlyMetadata(IDictionary<string, object>? metadata)
        => metadata is null
            ? null
            : new Dictionary<string, object>(metadata, StringComparer.OrdinalIgnoreCase);

    private static string BuildSnippet(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        if (normalized.Length <= 240)
            return normalized;

        var snippet = normalized[..240];
        var lastWhitespace = snippet.LastIndexOf(' ');
        if (lastWhitespace > 160)
            snippet = snippet[..lastWhitespace];
        return $"{snippet}...";
    }

    private sealed record IndexedChunk(
        RetrievalReadyDocument Document,
        CanonicalIngestionChunk Chunk);

    private sealed record SemanticMatch(
        float Score,
        IReadOnlyDictionary<string, object>? Metadata);
}
