using System.Text.RegularExpressions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Scrubs credential-shaped substrings from a skill body before any projector writes it. Per
/// SUC-16 SecurityNotes, every scrub action emits a <see cref="SecurityScrubReceipt"/> so
/// downstream (KnowledgeMemory, audit log) can replay <em>what fired and where</em> without leaking
/// the original value.
/// </summary>
/// <remarks>
/// Pattern catalog focuses on credential values that have hit DNA accidentally in the past:
/// OpenAI <c>sk-</c> keys, GitHub PATs (<c>ghp_</c>, <c>github_pat_</c>, etc.), generic Bearer
/// tokens after <c>Authorization:</c> headers, JWT-shaped strings, and GUIDs that appear next
/// to credential-context words (<c>password=</c>, <c>secret=</c>, <c>api[-_]?key=</c>). The
/// patterns are conservative — false positives are recorded as scrub receipts but the redacted
/// placeholder explicitly carries the pattern id so reviewers can resurrect the value from
/// CredentialsAgent or an audit-logged source if the scrub was wrong.
/// </remarks>
internal sealed class CredentialPatternRedactor
{
    private static readonly (string Id, Regex Pattern)[] Patterns =
    [
        // OpenAI / Anthropic API keys
        ("openai_sk", new Regex(@"sk-[A-Za-z0-9_-]{20,}", RegexOptions.Compiled)),
        ("anthropic_sk", new Regex(@"sk-ant-[A-Za-z0-9_-]{20,}", RegexOptions.Compiled)),
        // GitHub PATs / fine-grained tokens
        ("github_pat_legacy", new Regex(@"ghp_[A-Za-z0-9]{20,}", RegexOptions.Compiled)),
        ("github_pat", new Regex(@"github_pat_[A-Za-z0-9_]{20,}", RegexOptions.Compiled)),
        // Chat bot tokens
        ("chat_token", new Regex(@"xox[abprs]-[A-Za-z0-9-]{10,}", RegexOptions.Compiled)),
        // Authorization: Bearer <token>
        ("bearer", new Regex(@"(?<=Authorization:\s*Bearer\s+)[A-Za-z0-9._~+/=-]{20,}", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        // X-Api-Key: <value> — match a value that's at least 24 chars to avoid scrubbing example placeholders
        ("x_api_key", new Regex(@"(?<=X-Api-Key:\s*)[A-Za-z0-9._~+/=-]{24,}", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        // JWT-shaped tokens (three base64url segments separated by dots, each at least 4 chars)
        ("jwt", new Regex(@"eyJ[A-Za-z0-9_-]{4,}\.[A-Za-z0-9_-]{4,}\.[A-Za-z0-9_-]{4,}", RegexOptions.Compiled)),
    ];

    /// <summary>
    /// Returns the scrubbed text and a receipt per redacted span. When no patterns fire the
    /// returned string is the same instance as <paramref name="text"/> (reference-equal) so
    /// callers can cheaply detect "nothing changed".
    /// </summary>
    public (string Scrubbed, IReadOnlyList<SecurityScrubReceipt> Receipts) Scrub(string text, string source)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        // Collect matches across all patterns, then apply highest-priority match per character.
        var matches = new List<(int Start, int End, string PatternId)>();
        foreach (var (id, pattern) in Patterns)
        {
            foreach (Match match in pattern.Matches(text))
            {
                matches.Add((match.Index, match.Index + match.Length, id));
            }
        }
        if (matches.Count == 0)
        {
            return (text, Array.Empty<SecurityScrubReceipt>());
        }

        // Sort by start ascending. Resolve overlaps by keeping the earliest, longest match.
        matches.Sort((a, b) =>
        {
            var byStart = a.Start.CompareTo(b.Start);
            if (byStart != 0) return byStart;
            return b.End.CompareTo(a.End);
        });

        var sb = new System.Text.StringBuilder(text.Length);
        var receipts = new List<SecurityScrubReceipt>();
        var cursor = 0;
        foreach (var (start, end, id) in matches)
        {
            if (start < cursor) continue; // skip overlap
            if (start > cursor)
            {
                sb.Append(text, cursor, start - cursor);
            }
            var placeholder = $"***REDACTED:{id}***";
            sb.Append(placeholder);
            receipts.Add(new SecurityScrubReceipt(
                PatternId: id,
                Source: source,
                MatchedRange: (start, end),
                RedactedPlaceholder: placeholder));
            cursor = end;
        }
        if (cursor < text.Length)
        {
            sb.Append(text, cursor, text.Length - cursor);
        }
        return (sb.ToString(), receipts);
    }
}
