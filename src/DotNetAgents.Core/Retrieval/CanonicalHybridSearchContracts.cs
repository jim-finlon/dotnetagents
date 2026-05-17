using DotNetAgents.Abstractions.Retrieval;

namespace DotNetAgents.Core.Retrieval;

public enum HybridSearchMode
{
    Lexical = 0,
    Semantic = 1,
    Hybrid = 2
}

public sealed record CanonicalHybridSearchQuery(
    string QueryText,
    HybridSearchMode Mode = HybridSearchMode.Hybrid,
    float[]? QueryVector = null,
    int TopK = 10,
    IReadOnlyCollection<string>? AllowedSourceClasses = null,
    IReadOnlyCollection<string>? AllowedSourceIds = null,
    IReadOnlyDictionary<string, object>? SemanticFilter = null);

public sealed record CanonicalHybridSearchHit(
    string DocumentId,
    string ChunkId,
    string SourceClass,
    string SourceId,
    string Title,
    string Snippet,
    int Rank,
    float LexicalScore,
    float SemanticScore,
    float CombinedScore,
    IReadOnlyList<string> HitExplanations,
    IReadOnlyDictionary<string, string> Provenance,
    IReadOnlyDictionary<string, object>? SemanticMetadata);

public sealed record CanonicalHybridSearchIndexSummary(
    DateTimeOffset RefreshedAtUtc,
    int DocumentCount,
    int ChunkCount,
    IReadOnlyList<CanonicalSourceSupport> SupportedSourceClasses);

public sealed record CanonicalHybridSearchResponse(
    HybridSearchMode ModeUsed,
    CanonicalHybridSearchIndexSummary IndexSummary,
    IReadOnlyList<CanonicalHybridSearchHit> Hits);

public interface ICanonicalHybridSearchService
{
    CanonicalHybridSearchIndexSummary GetIndexSummary();

    Task<CanonicalHybridSearchResponse> SearchAsync(
        CanonicalHybridSearchQuery query,
        CancellationToken cancellationToken = default);
}
