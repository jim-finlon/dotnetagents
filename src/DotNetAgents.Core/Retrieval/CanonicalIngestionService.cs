using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetAgents.Core.Retrieval;

public sealed class CanonicalIngestionService
{
    private static readonly string[] SupportedTextExtensions =
    [
        ".md", ".markdown", ".txt", ".json", ".yml", ".yaml", ".cs", ".csproj", ".ps1", ".sh", ".html", ".htm"
    ];

    private static readonly IReadOnlyDictionary<string, string> DownstreamContracts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["embedding"] = "dna.embedding.document.v1",
        ["reranking"] = "dna.reranking.candidate.v1",
        ["retrieval"] = "dna.retrieval.document.v1"
    };

    private readonly ICanonicalIngestionCheckpointStore _checkpointStore;
    private readonly ILogger<CanonicalIngestionService> _logger;

    public CanonicalIngestionService(
        ICanonicalIngestionCheckpointStore? checkpointStore = null,
        ILogger<CanonicalIngestionService>? logger = null)
    {
        _checkpointStore = checkpointStore ?? new InMemoryCanonicalIngestionCheckpointStore();
        _logger = logger ?? NullLogger<CanonicalIngestionService>.Instance;
    }

    public IReadOnlyList<CanonicalSourceSupport> GetSupportedSourceTypes() =>
    [
        new(
            CanonicalIngestionSourceClasses.Repository,
            "partial",
            "Walk text-backed repository files, normalize line endings, hash per file, deduplicate by content hash, and chunk aggregate retrieval text.",
            "Binary assets are surfaced as unsupported artifacts instead of being silently dropped."),
        new(
            CanonicalIngestionSourceClasses.Documentation,
            "supported",
            "Accept markdown/plain-text files or directories, normalize headings and whitespace, then produce retrieval-ready chunks.",
            "Current slice is optimized for text-backed docs; OCR-backed binaries remain a follow-up story."),
        new(
            CanonicalIngestionSourceClasses.Transcript,
            "supported",
            "Accept transcript text directly or from a text file, preserve source lineage, and chunk by paragraph boundaries where possible.",
            "Audio transcription itself stays outside this service; this service owns canonical post-transcription ingestion."),
        new(
            CanonicalIngestionSourceClasses.FileArtifact,
            "partial",
            "Accept text-backed artifacts directly and make unsupported binary/file types explicit.",
            "Binary extraction hooks are left for OCR/file-extraction follow-up work.")
    ];

    public async Task<CanonicalIngestionResult> IngestAsync(
        CanonicalIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceId);

        if (!CanonicalIngestionSourceClasses.All.Contains(request.SourceClass))
            throw new ArgumentOutOfRangeException(nameof(request), $"Unsupported source class '{request.SourceClass}'.");

        var warnings = new List<string>();
        var unsupportedArtifacts = new List<string>();
        var artifacts = await ResolveArtifactsAsync(request, unsupportedArtifacts, cancellationToken).ConfigureAwait(false);
        if (artifacts.Count == 0)
        {
            throw new InvalidOperationException(
                $"Canonical ingestion could not resolve any supported artifacts for source '{request.SourceId}'. Unsupported artifacts: {string.Join(", ", unsupportedArtifacts)}");
        }

        var deduplicated = artifacts
            .GroupBy(static artifact => artifact.ContentHash, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToArray();
        if (deduplicated.Length != artifacts.Count)
            warnings.Add($"Deduplicated {artifacts.Count - deduplicated.Length} duplicate artifact(s) by content hash.");
        if (unsupportedArtifacts.Count > 0)
            warnings.Add($"Skipped {unsupportedArtifacts.Count} unsupported artifact(s).");

        var normalizedText = NormalizeAggregateText(deduplicated);
        var title = request.DisplayName
            ?? InferTitle(request.SourceId, request.SourceClass, deduplicated);
        var checkpointKey = request.CheckpointKey
            ?? $"{request.SourceClass}:{request.SourceId}".ToLowerInvariant();
        var sourceFingerprint = ComputeHash(string.Join("\n", deduplicated.Select(static artifact => $"{artifact.RelativeSourceId}|{artifact.ContentHash}")));
        var normalizedHash = ComputeHash(normalizedText);
        var dedupKey = $"{request.SourceClass}:{normalizedHash}";
        var now = DateTimeOffset.UtcNow;

        if (_checkpointStore.TryGet(checkpointKey, out var checkpoint) &&
            checkpoint != null &&
            string.Equals(checkpoint.SourceFingerprint, sourceFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            var unchangedRecord = new CanonicalIngestionRecord(
                DocumentId: dedupKey,
                SourceClass: request.SourceClass,
                SourceId: request.SourceId,
                Title: title,
                CheckpointKey: checkpointKey,
                SourceFingerprint: sourceFingerprint,
                NormalizedHash: normalizedHash,
                DedupKey: dedupKey,
                Status: "unchanged",
                ArtifactCount: artifacts.Count,
                DeduplicatedArtifactCount: deduplicated.Length,
                ProcessedAtUtc: now,
                Lineage: BuildLineage(request, checkpointKey, sourceFingerprint, normalizedHash, deduplicated));

            return new CanonicalIngestionResult(unchangedRecord, null, warnings, unsupportedArtifacts);
        }

        var chunks = BuildChunks(normalizedText, request.SourceClass, request.SourceId, dedupKey);
        var lineage = BuildLineage(request, checkpointKey, sourceFingerprint, normalizedHash, deduplicated);
        var retrievalReady = new RetrievalReadyDocument(
            DocumentId: dedupKey,
            SourceClass: request.SourceClass,
            SourceId: request.SourceId,
            Title: title,
            NormalizedHash: normalizedHash,
            DedupKey: dedupKey,
            CheckpointKey: checkpointKey,
            Lineage: lineage,
            Chunks: chunks,
            DownstreamContracts: DownstreamContracts);

        var record = new CanonicalIngestionRecord(
            DocumentId: dedupKey,
            SourceClass: request.SourceClass,
            SourceId: request.SourceId,
            Title: title,
            CheckpointKey: checkpointKey,
            SourceFingerprint: sourceFingerprint,
            NormalizedHash: normalizedHash,
            DedupKey: dedupKey,
            Status: "ingested",
            ArtifactCount: artifacts.Count,
            DeduplicatedArtifactCount: deduplicated.Length,
            ProcessedAtUtc: now,
            Lineage: lineage);

        _checkpointStore.Upsert(new CanonicalIngestionCheckpoint(checkpointKey, sourceFingerprint, normalizedHash, now));
        _logger.LogInformation(
            "Canonical ingestion stored {ChunkCount} retrieval-ready chunks for {SourceClass}:{SourceId}",
            chunks.Count,
            request.SourceClass,
            request.SourceId);

        return new CanonicalIngestionResult(record, retrievalReady, warnings, unsupportedArtifacts);
    }

    private static IReadOnlyDictionary<string, string> BuildLineage(
        CanonicalIngestionRequest request,
        string checkpointKey,
        string sourceFingerprint,
        string normalizedHash,
        IReadOnlyList<ResolvedArtifact> artifacts)
    {
        var lineage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceClass"] = request.SourceClass,
            ["sourceId"] = request.SourceId,
            ["checkpointKey"] = checkpointKey,
            ["sourceFingerprint"] = sourceFingerprint,
            ["normalizedHash"] = normalizedHash,
            ["artifactCount"] = artifacts.Count.ToString(),
            ["artifactList"] = string.Join(";", artifacts.Select(static artifact => artifact.RelativeSourceId))
        };

        if (request.Metadata != null)
        {
            foreach (var pair in request.Metadata)
                lineage[$"meta:{pair.Key}"] = pair.Value;
        }

        return lineage;
    }

    private static string InferTitle(string sourceId, string sourceClass, IReadOnlyList<ResolvedArtifact> artifacts)
    {
        if (artifacts.Count == 1)
            return Path.GetFileNameWithoutExtension(artifacts[0].RelativeSourceId);

        return $"{sourceClass.Replace('_', ' ')}:{Path.GetFileName(sourceId.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}";
    }

    private static string NormalizeAggregateText(IReadOnlyList<ResolvedArtifact> artifacts)
    {
        var builder = new StringBuilder();
        foreach (var artifact in artifacts)
        {
            if (builder.Length > 0)
                builder.Append("\n\n");

            builder.Append("# Source: ").AppendLine(artifact.RelativeSourceId);
            builder.AppendLine();
            builder.AppendLine(artifact.NormalizedText.Trim());
        }

        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }

    private static IReadOnlyList<CanonicalIngestionChunk> BuildChunks(
        string normalizedText,
        string sourceClass,
        string sourceId,
        string dedupKey)
    {
        const int maxChars = 1200;
        const int overlap = 150;

        var chunks = new List<CanonicalIngestionChunk>();
        var paragraphs = normalizedText
            .Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var current = new StringBuilder();
        var ordinal = 0;
        foreach (var paragraph in paragraphs)
        {
            if (current.Length > 0 && current.Length + paragraph.Length + 2 > maxChars)
            {
                chunks.Add(CreateChunk(dedupKey, ordinal++, current.ToString(), sourceClass, sourceId));
                var carry = current.Length > overlap
                    ? current.ToString()[Math.Max(0, current.Length - overlap)..]
                    : current.ToString();
                current.Clear();
                current.Append(carry.Trim());
            }

            if (current.Length > 0)
                current.Append("\n\n");
            current.Append(paragraph.Trim());
        }

        if (current.Length > 0)
            chunks.Add(CreateChunk(dedupKey, ordinal, current.ToString(), sourceClass, sourceId));

        return chunks;
    }

    private static CanonicalIngestionChunk CreateChunk(string dedupKey, int ordinal, string text, string sourceClass, string sourceId)
    {
        var normalized = text.Trim();
        return new CanonicalIngestionChunk(
            ChunkId: $"{dedupKey}:chunk:{ordinal:D4}",
            Ordinal: ordinal,
            Text: normalized,
            CharacterCount: normalized.Length,
            TokenEstimate: Math.Max(1, normalized.Length / 4),
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceClass"] = sourceClass,
                ["sourceId"] = sourceId
            });
    }

    private static string ComputeHash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private async Task<IReadOnlyList<ResolvedArtifact>> ResolveArtifactsAsync(
        CanonicalIngestionRequest request,
        List<string> unsupportedArtifacts,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ContentText))
        {
            return
            [
                CreateResolvedArtifact(request.SourceId, request.ContentText)
            ];
        }

        if (File.Exists(request.SourceId))
        {
            if (!IsSupportedTextFile(request.SourceId))
            {
                unsupportedArtifacts.Add(request.SourceId);
                return Array.Empty<ResolvedArtifact>();
            }

            var content = await File.ReadAllTextAsync(request.SourceId, cancellationToken).ConfigureAwait(false);
            return
            [
                CreateResolvedArtifact(request.SourceId, content)
            ];
        }

        if (Directory.Exists(request.SourceId))
        {
            var artifacts = new List<ResolvedArtifact>();
            foreach (var path in Directory.EnumerateFiles(request.SourceId, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsSupportedTextFile(path))
                {
                    unsupportedArtifacts.Add(Path.GetRelativePath(request.SourceId, path));
                    continue;
                }

                var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                artifacts.Add(CreateResolvedArtifact(Path.GetRelativePath(request.SourceId, path), content));
            }

            return artifacts;
        }

        throw new FileNotFoundException($"Source '{request.SourceId}' was not found as a file, directory, or inline content payload.");
    }

    private static bool IsSupportedTextFile(string path)
        => SupportedTextExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static ResolvedArtifact CreateResolvedArtifact(string relativeSourceId, string content)
    {
        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        return new ResolvedArtifact(
            relativeSourceId,
            normalized,
            ComputeHash(normalized));
    }

    private sealed record ResolvedArtifact(
        string RelativeSourceId,
        string NormalizedText,
        string ContentHash);
}
