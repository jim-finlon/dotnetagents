// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace DotNetAgents.Core.Retrieval;

public static class CanonicalIngestionSourceClasses
{
    public const string Repository = "repository";
    public const string Documentation = "documentation";
    public const string Transcript = "transcript";
    public const string FileArtifact = "file_artifact";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Repository,
        Documentation,
        Transcript,
        FileArtifact
    };
}

public sealed record CanonicalIngestionRequest(
    string SourceClass,
    string SourceId,
    string? DisplayName = null,
    string? ContentText = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    string? CheckpointKey = null);

public sealed record CanonicalSourceSupport(
    string SourceClass,
    string SupportLevel,
    string NormalizationRule,
    string Notes);

public sealed record CanonicalIngestionChunk(
    string ChunkId,
    int Ordinal,
    string Text,
    int CharacterCount,
    int TokenEstimate,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record RetrievalReadyDocument(
    string DocumentId,
    string SourceClass,
    string SourceId,
    string Title,
    string NormalizedHash,
    string DedupKey,
    string CheckpointKey,
    IReadOnlyDictionary<string, string> Lineage,
    IReadOnlyList<CanonicalIngestionChunk> Chunks,
    IReadOnlyDictionary<string, string> DownstreamContracts);

public sealed record CanonicalIngestionRecord(
    string DocumentId,
    string SourceClass,
    string SourceId,
    string Title,
    string CheckpointKey,
    string SourceFingerprint,
    string NormalizedHash,
    string DedupKey,
    string Status,
    int ArtifactCount,
    int DeduplicatedArtifactCount,
    DateTimeOffset ProcessedAtUtc,
    IReadOnlyDictionary<string, string> Lineage);

public sealed record CanonicalIngestionResult(
    CanonicalIngestionRecord Record,
    RetrievalReadyDocument? RetrievalReadyDocument,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> UnsupportedArtifacts);

public sealed record CanonicalIngestionCheckpoint(
    string CheckpointKey,
    string SourceFingerprint,
    string NormalizedHash,
    DateTimeOffset ProcessedAtUtc);

public interface ICanonicalIngestionCheckpointStore
{
    bool TryGet(string checkpointKey, out CanonicalIngestionCheckpoint? checkpoint);
    void Upsert(CanonicalIngestionCheckpoint checkpoint);
}

public sealed class InMemoryCanonicalIngestionCheckpointStore : ICanonicalIngestionCheckpointStore
{
    private readonly ConcurrentDictionary<string, CanonicalIngestionCheckpoint> _entries = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string checkpointKey, out CanonicalIngestionCheckpoint? checkpoint)
    {
        if (_entries.TryGetValue(checkpointKey, out var found))
        {
            checkpoint = found;
            return true;
        }

        checkpoint = null;
        return false;
    }

    public void Upsert(CanonicalIngestionCheckpoint checkpoint)
        => _entries[checkpoint.CheckpointKey] = checkpoint;
}
