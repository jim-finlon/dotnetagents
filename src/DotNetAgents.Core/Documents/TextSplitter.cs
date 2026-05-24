// SPDX-License-Identifier: Apache-2.0

using System.Text;
using DotNetAgents.Abstractions.Documents;

namespace DotNetAgents.Core.Documents;

/// <summary>
/// Splits text by character count with optional overlap.
/// </summary>
public class CharacterTextSplitter : ITextSplitter
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;
    private readonly string[] _separators;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterTextSplitter"/> class.
    /// </summary>
    /// <param name="chunkSize">The maximum size of each chunk in characters.</param>
    /// <param name="chunkOverlap">The number of characters to overlap between chunks.</param>
    /// <param name="separators">Optional separators to split on (defaults to newlines).</param>
    public CharacterTextSplitter(
        int chunkSize = 1000,
        int chunkOverlap = 200,
        string[]? separators = null)
    {
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be positive.", nameof(chunkSize));
        if (chunkOverlap < 0)
            throw new ArgumentException("Chunk overlap must be non-negative.", nameof(chunkOverlap));
        if (chunkOverlap >= chunkSize)
            throw new ArgumentException("Chunk overlap must be less than chunk size.", nameof(chunkOverlap));

        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
        _separators = separators ?? new[] { "\n\n", "\n", " ", "" };
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Document>> SplitAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        return Task.FromResult(SplitDocument(document));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Document>> SplitDocumentsAsync(
        IEnumerable<Document> documents,
        CancellationToken cancellationToken = default)
    {
        if (documents == null)
            throw new ArgumentNullException(nameof(documents));

        var allChunks = new List<Document>();
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunks = await SplitAsync(document, cancellationToken).ConfigureAwait(false);
            allChunks.AddRange(chunks);
        }

        return allChunks;
    }

    private IReadOnlyList<Document> SplitDocument(Document document)
    {
        var text = document.Content;
        var chunks = new List<Document>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return chunks;
        }

        // Try to split by separators first
        var splits = SplitText(text);

        var currentChunk = new StringBuilder();
        var currentLength = 0;

        foreach (var split in splits)
        {
            if (currentLength + split.Length > _chunkSize && currentChunk.Length > 0)
            {
                // Save current chunk
                chunks.Add(CreateChunk(document, currentChunk.ToString(), chunks.Count));

                // Start new chunk with overlap
                var overlapText = GetOverlapText(currentChunk.ToString());
                currentChunk.Clear();
                currentChunk.Append(overlapText);
                currentLength = overlapText.Length;
            }

            currentChunk.Append(split);
            currentLength += split.Length;
        }

        // Add final chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(CreateChunk(document, currentChunk.ToString(), chunks.Count));
        }

        return chunks;
    }

    private List<string> SplitText(string text)
    {
        var splits = new List<string> { text };

        foreach (var separator in _separators)
        {
            var newSplits = new List<string>();
            foreach (var split in splits)
            {
                if (string.IsNullOrEmpty(separator))
                {
                    // Character-level splitting
                    newSplits.AddRange(split.ToCharArray().Select(c => c.ToString()));
                }
                else
                {
                    var parts = split.Split(new[] { separator }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        newSplits.Add(parts[i]);
                        if (i < parts.Length - 1)
                        {
                            newSplits.Add(separator);
                        }
                    }
                }
            }
            splits = newSplits;
        }

        return splits;
    }

    private string GetOverlapText(string text)
    {
        if (_chunkOverlap == 0 || text.Length <= _chunkOverlap)
        {
            return string.Empty;
        }

        return text.Substring(text.Length - _chunkOverlap);
    }

    private static Document CreateChunk(Document original, string content, int chunkIndex)
    {
        var metadata = new Dictionary<string, object>(original.Metadata)
        {
            ["chunk_index"] = chunkIndex
        };

        return new Document
        {
            Content = content,
            Metadata = metadata,
            PageNumber = original.PageNumber
        };
    }
}
