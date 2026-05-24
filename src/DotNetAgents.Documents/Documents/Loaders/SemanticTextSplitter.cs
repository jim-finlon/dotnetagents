// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Documents;
using DotNetAgents.Abstractions.Models;


namespace DotNetAgents.Documents.Loaders;

/// <summary>
/// Semantic text splitter that uses embeddings to group semantically similar text together.
/// This approach creates chunks that are more coherent and contextually meaningful.
/// </summary>
public class SemanticTextSplitter : ITextSplitter
{
    private readonly IEmbeddingModel _embeddingModel;
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;
    private readonly double _similarityThreshold;
    private readonly ITextSplitter _baseSplitter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticTextSplitter"/> class.
    /// </summary>
    /// <param name="embeddingModel">The embedding model to use for semantic similarity calculations.</param>
    /// <param name="chunkSize">The target size of each chunk in characters.</param>
    /// <param name="chunkOverlap">The number of characters to overlap between chunks.</param>
    /// <param name="similarityThreshold">The similarity threshold for grouping chunks. Default: 0.7.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="embeddingModel"/> is null.</exception>
    public SemanticTextSplitter(
        IEmbeddingModel embeddingModel,
        int chunkSize = 1000,
        int chunkOverlap = 200,
        double similarityThreshold = 0.7)
    {
        _embeddingModel = embeddingModel ?? throw new ArgumentNullException(nameof(embeddingModel));

        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive.", nameof(chunkSize));
        if (chunkOverlap < 0)
            throw new ArgumentException("Chunk overlap must be non-negative.", nameof(chunkOverlap));
        if (chunkOverlap >= chunkSize)
            throw new ArgumentException("Chunk overlap must be less than chunk size.", nameof(chunkOverlap));
        if (similarityThreshold < 0 || similarityThreshold > 1)
            throw new ArgumentException("Similarity threshold must be between 0 and 1.", nameof(similarityThreshold));

        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
        _similarityThreshold = similarityThreshold;

        // Use recursive splitter as base to create initial chunks
        _baseSplitter = new RecursiveTextSplitter(chunkSize, chunkOverlap);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Document>> SplitDocumentsAsync(
        IEnumerable<Document> documents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documents);

        var result = new List<Document>();
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // First, split using base splitter
            var baseChunks = await _baseSplitter.SplitDocumentsAsync(
                new[] { document },
                cancellationToken).ConfigureAwait(false);

            // Then, merge semantically similar chunks
            var semanticChunks = await MergeSemanticChunksAsync(
                baseChunks,
                cancellationToken).ConfigureAwait(false);

            result.AddRange(semanticChunks);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> SplitTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var document = new Document { Content = text };
        var chunks = await SplitDocumentsAsync(
            new[] { document },
            cancellationToken).ConfigureAwait(false);

        return chunks.Select(c => c.Content).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Document>> SplitAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var chunks = await SplitDocumentsAsync(
            new[] { document },
            cancellationToken).ConfigureAwait(false);

        return chunks;
    }

    private async Task<List<Document>> MergeSemanticChunksAsync(
        IReadOnlyList<Document> chunks,
        CancellationToken cancellationToken)
    {
        if (chunks.Count <= 1)
            return chunks.ToList();

        // Generate embeddings for all chunks
        var chunkTexts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingModel.EmbedBatchAsync(
            chunkTexts,
            cancellationToken).ConfigureAwait(false);

        var mergedChunks = new List<Document>();
        var currentChunk = new List<(Document Doc, float[] Embedding)>();
        var currentLength = 0;

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var embedding = embeddings[i];

            // Check if we should merge with previous chunks
            bool shouldMerge = false;
            if (currentChunk.Count > 0)
            {
                // Calculate average similarity with current chunk group
                var similarities = currentChunk.Select(c =>
                    CosineSimilarity(c.Embedding, embedding)).ToList();
                var avgSimilarity = similarities.Average();

                if (avgSimilarity >= _similarityThreshold &&
                    currentLength + chunk.Content.Length <= _chunkSize)
                {
                    shouldMerge = true;
                }
            }

            if (shouldMerge && currentChunk.Count > 0)
            {
                // Merge with current chunk
                currentChunk.Add((chunk, embedding));
                currentLength += chunk.Content.Length;
            }
            else
            {
                // Start a new chunk
                if (currentChunk.Count > 0)
                {
                    // Save previous chunk group
                    var mergedContent = string.Join("\n\n",
                        currentChunk.Select(c => c.Doc.Content));
                    var mergedMetadata = MergeMetadata(currentChunk.Select(c => c.Doc));
                    mergedChunks.Add(new Document
                    {
                        Content = mergedContent,
                        Metadata = mergedMetadata
                    });
                }

                currentChunk = new List<(Document, float[])> { (chunk, embedding) };
                currentLength = chunk.Content.Length;
            }
        }

        // Add final chunk
        if (currentChunk.Count > 0)
        {
            var mergedContent = string.Join("\n\n",
                currentChunk.Select(c => c.Doc.Content));
            var mergedMetadata = MergeMetadata(currentChunk.Select(c => c.Doc));
            mergedChunks.Add(new Document
            {
                Content = mergedContent,
                Metadata = mergedMetadata
            });
        }

        return mergedChunks;
    }

    private static IDictionary<string, object> MergeMetadata(IEnumerable<Document> chunks)
    {
        var metadata = new Dictionary<string, object>();
        var chunkList = chunks.ToList();

        if (chunkList.Count > 0)
        {
            // Copy base metadata from first chunk
            foreach (var kvp in chunkList[0].Metadata)
            {
                if (kvp.Key != "chunk_index" && kvp.Key != "chunk_size")
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            metadata["semantic_chunk"] = true;
            metadata["merged_chunks"] = chunkList.Count;
            metadata["total_length"] = chunkList.Sum(c => c.Content.Length);
        }

        return metadata;
    }

    private static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        // Use optimized SIMD implementation and convert to double
        return DotNetAgents.Core.Retrieval.VectorOperations.CosineSimilarity(vectorA, vectorB);
    }
}
