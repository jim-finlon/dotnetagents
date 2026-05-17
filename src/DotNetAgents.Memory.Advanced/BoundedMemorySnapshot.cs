using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetAgents.Memory.Advanced;

public sealed record MemorySnapshotRequest(
    string Scope,
    string Source,
    int MaxCharacters,
    DateTimeOffset? LastReviewedAtUtc = null,
    string PolicyId = "bounded-prompt-memory-v1");

public sealed record MemorySnapshotSourceRecord(
    string Id,
    string Content,
    double Importance = 0.5,
    DateTimeOffset? ObservedAtUtc = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record MemorySnapshot(
    string Scope,
    string Source,
    int MaxCharacters,
    string Content,
    string ContentHash,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset? LastReviewedAtUtc,
    string PolicyId,
    string RedactionReceipt,
    string AuditReceipt,
    IReadOnlyList<string> SourceRecordIds)
{
    public int CharacterCount => Content.Length;
}

public interface IMemorySnapshotProvider
{
    Task<MemorySnapshot> BuildSnapshotAsync(
        MemorySnapshotRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record MemoryTranscriptSearchQuery(
    string Text,
    string? Scope = null,
    string? SessionId = null,
    int Limit = 10);

public sealed record MemoryTranscriptSearchResult(
    string SessionId,
    string MessageId,
    string Scope,
    string Snippet,
    double Score,
    DateTimeOffset ObservedAtUtc);

public interface IMemorySearchProvider
{
    Task<IReadOnlyList<MemoryTranscriptSearchResult>> SearchAsync(
        MemoryTranscriptSearchQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record MemoryWriteCandidate(
    string Scope,
    string Content,
    int MaxCharacters,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public enum MemoryWritePolicyOutcome
{
    Accepted,
    Flagged,
    Rejected
}

public sealed record MemoryWritePolicyDecision(
    MemoryWritePolicyOutcome Outcome,
    string PolicyId,
    IReadOnlyList<string> Reasons)
{
    public bool CanWrite => Outcome is MemoryWritePolicyOutcome.Accepted or MemoryWritePolicyOutcome.Flagged;
}

public interface IMemoryWritePolicy
{
    MemoryWritePolicyDecision Evaluate(MemoryWriteCandidate candidate);
}

public interface IMemoryLeakScrubber
{
    string Scrub(string output);
}

public sealed class InMemoryBoundedMemorySnapshotProvider : IMemorySnapshotProvider
{
    private readonly IReadOnlyList<MemorySnapshotSourceRecord> _records;
    private readonly IMemoryLeakScrubber _scrubber;
    private readonly TimeProvider _timeProvider;

    public InMemoryBoundedMemorySnapshotProvider(
        IEnumerable<MemorySnapshotSourceRecord> records,
        IMemoryLeakScrubber? scrubber = null,
        TimeProvider? timeProvider = null)
    {
        _records = records?.ToArray() ?? throw new ArgumentNullException(nameof(records));
        _scrubber = scrubber ?? new ProviderTagMemoryLeakScrubber();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<MemorySnapshot> BuildSnapshotAsync(
        MemorySnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.MaxCharacters <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "MaxCharacters must be positive.");

        var selected = _records
            .Where(record => AppliesToScope(record, request.Scope))
            .OrderByDescending(record => record.Importance)
            .ThenByDescending(record => record.ObservedAtUtc ?? DateTimeOffset.MinValue)
            .ToArray();

        var builder = new StringBuilder(capacity: Math.Min(request.MaxCharacters, 4096));
        var sourceIds = new List<string>();
        var redactions = 0;

        foreach (var record in selected)
        {
            var clean = _scrubber.Scrub(record.Content).Trim();
            if (clean.Length != record.Content.Trim().Length)
                redactions++;

            if (string.IsNullOrWhiteSpace(clean))
                continue;

            var line = $"- {clean}";
            var separator = builder.Length == 0 ? 0 : Environment.NewLine.Length;
            if (builder.Length + separator + line.Length > request.MaxCharacters)
                continue;

            if (separator > 0)
                builder.AppendLine();

            builder.Append(line);
            sourceIds.Add(record.Id);
        }

        var content = builder.ToString();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
        var capturedAt = _timeProvider.GetUtcNow();
        return Task.FromResult(new MemorySnapshot(
            request.Scope,
            request.Source,
            request.MaxCharacters,
            content,
            hash,
            capturedAt,
            request.LastReviewedAtUtc,
            request.PolicyId,
            redactions == 0 ? "none" : $"provider_private_blocks_removed:{redactions}",
            $"snapshot:{request.Scope}:{capturedAt:O}:{sourceIds.Count}",
            sourceIds));
    }

    private static bool AppliesToScope(MemorySnapshotSourceRecord record, string scope)
    {
        if (record.Metadata is not null
            && record.Metadata.TryGetValue("scope", out var recordScope)
            && !string.Equals(recordScope, scope, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}

public sealed class InMemoryTranscriptSearchProvider : IMemorySearchProvider
{
    private readonly IReadOnlyList<MemoryTranscriptDocument> _documents;

    public InMemoryTranscriptSearchProvider(IEnumerable<MemoryTranscriptDocument> documents)
    {
        _documents = documents?.ToArray() ?? throw new ArgumentNullException(nameof(documents));
    }

    public Task<IReadOnlyList<MemoryTranscriptSearchResult>> SearchAsync(
        MemoryTranscriptSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query.Text))
            return Task.FromResult<IReadOnlyList<MemoryTranscriptSearchResult>>(Array.Empty<MemoryTranscriptSearchResult>());

        var terms = query.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var limit = query.Limit <= 0 ? 10 : query.Limit;
        var results = _documents
            .Where(document => query.Scope is null || string.Equals(document.Scope, query.Scope, StringComparison.OrdinalIgnoreCase))
            .Where(document => query.SessionId is null || string.Equals(document.SessionId, query.SessionId, StringComparison.OrdinalIgnoreCase))
            .Select(document => new
            {
                Document = document,
                Score = Score(document.Content, terms)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Document.ObservedAtUtc)
            .Take(limit)
            .Select(item => new MemoryTranscriptSearchResult(
                item.Document.SessionId,
                item.Document.MessageId,
                item.Document.Scope,
                BuildSnippet(item.Document.Content, terms[0]),
                item.Score,
                item.Document.ObservedAtUtc))
            .ToArray();

        return Task.FromResult<IReadOnlyList<MemoryTranscriptSearchResult>>(results);
    }

    private static double Score(string content, IReadOnlyList<string> terms)
    {
        var score = 0;
        foreach (var term in terms)
        {
            if (content.Contains(term, StringComparison.OrdinalIgnoreCase))
                score++;
        }

        return score / (double)terms.Count;
    }

    private static string BuildSnippet(string content, string term)
    {
        var index = content.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return content.Length <= 160 ? content : content[..160];

        var start = Math.Max(0, index - 40);
        var length = Math.Min(content.Length - start, 160);
        return content.Substring(start, length);
    }
}

public sealed record MemoryTranscriptDocument(
    string SessionId,
    string MessageId,
    string Scope,
    string Content,
    DateTimeOffset ObservedAtUtc);

public sealed class DefaultMemoryWritePolicy : IMemoryWritePolicy
{
    private static readonly Regex SecretPattern = new(
        @"(?i)(api[_-]?key|secret|token|password)\s*[:=]\s*\S+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _policyId;

    public DefaultMemoryWritePolicy(string policyId = "bounded-prompt-memory-write-v1")
    {
        _policyId = policyId;
    }

    public MemoryWritePolicyDecision Evaluate(MemoryWriteCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var reasons = new List<string>();

        if (string.IsNullOrWhiteSpace(candidate.Content))
            reasons.Add("low_value_chatter");
        if (candidate.Content.Trim().Length < 12)
            reasons.Add("low_value_chatter");
        if (candidate.Content.Length > candidate.MaxCharacters)
            reasons.Add("over_budget");
        if (SecretPattern.IsMatch(candidate.Content))
            reasons.Add("secret_like_material");
        if (candidate.Tags?.Any(tag => string.Equals(tag, "unstable-fact", StringComparison.OrdinalIgnoreCase)) == true)
            reasons.Add("unstable_fact");

        if (reasons.Contains("secret_like_material", StringComparer.OrdinalIgnoreCase))
            return new MemoryWritePolicyDecision(MemoryWritePolicyOutcome.Rejected, _policyId, reasons);

        if (reasons.Count > 0)
            return new MemoryWritePolicyDecision(MemoryWritePolicyOutcome.Flagged, _policyId, reasons.Distinct().ToArray());

        return new MemoryWritePolicyDecision(MemoryWritePolicyOutcome.Accepted, _policyId, Array.Empty<string>());
    }
}

public sealed class ProviderTagMemoryLeakScrubber : IMemoryLeakScrubber
{
    private static readonly Regex ProviderBlockPattern = new(
        @"<memory-provider\b[^>]*>.*?</memory-provider>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex InlineTagPattern = new(
        @"\[\[(?:memory|provider):[^\]]+\]\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public string Scrub(string output)
    {
        if (string.IsNullOrEmpty(output))
            return string.Empty;

        var withoutBlocks = ProviderBlockPattern.Replace(output, string.Empty);
        return InlineTagPattern.Replace(withoutBlocks, string.Empty).Trim();
    }
}
