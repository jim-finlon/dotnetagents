// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using DotNetAgents.Abstractions.CounterAgents;

namespace DotNetAgents.Core.CounterAgents;

/// <summary>
/// Reference counter-agent that flags actions matching known-dangerous patterns: destructive
/// shell commands, force-push to protected branches, schema-drop SQL, broad file deletion.
/// Pattern lists are operator-extensible via <see cref="HighConfidencePatterns"/> +
/// <see cref="CautionPatterns"/> plus the constructor's additional-patterns parameters.
/// </summary>
/// <remarks>
/// Deterministic, rule-based, zero-LLM. The pattern list is intentionally conservative — false
/// positives are surfaced as Concern (not Block) for low-confidence matches; only patterns
/// matching <see cref="HighConfidencePatterns"/> escalate to Block.
/// </remarks>
public sealed class SafetyCounterAgent : ICounterAgent
{
    /// <summary>
    /// Patterns that match obviously-destructive operations. Match → Block (Critical severity).
    /// Operators can extend at construction time.
    /// </summary>
    public static readonly IReadOnlyList<string> HighConfidencePatterns = new[]
    {
        @"\brm\s+-rf\s+(/|~|\$HOME)",                    // rm -rf /  rm -rf ~  rm -rf $HOME (also paths starting with these)
        @"\bgit\s+push\s+(--force|-f)\b.*\b(main|master)\b", // force-push main/master
        @"\bdrop\s+(database|schema)\b",                 // drop database, drop schema
        @"\btruncate\s+table\b",                         // truncate table
        @"\bdelete\s+from\s+\w+",                        // delete from X (without where — caught even with semicolon)
        @"\bgcloud\s+\w+\s+delete\b",                    // gcloud anything delete
        @"\baws\s+s3\s+rb\b",                            // aws s3 rb (remove bucket)
        @":\(\)\s*\{\s*:\|:&\s*\};\s*:",                 // fork bomb :(){ :|:& };:
    };

    /// <summary>
    /// Patterns that indicate caution but not certainty. Match → Concern (Major severity).
    /// </summary>
    public static readonly IReadOnlyList<string> CautionPatterns = new[]
    {
        @"\bsudo\s+rm\b",                  // sudo rm (without -rf, still elevated delete)
        @"\bchmod\s+777\b",                // chmod 777
        @"\bcurl\s+.*\|\s*(bash|sh)\b",    // curl | bash
        @"\beval\s*\(",                    // eval(
        @"\bunsafe\b",                     // explicit unsafe markers
    };

    private readonly IReadOnlyList<Regex> _highConfidence;
    private readonly IReadOnlyList<Regex> _caution;

    public SafetyCounterAgent(
        IEnumerable<string>? additionalHighConfidencePatterns = null,
        IEnumerable<string>? additionalCautionPatterns = null)
    {
        var highConf = HighConfidencePatterns.Concat(additionalHighConfidencePatterns ?? Array.Empty<string>());
        var caution = CautionPatterns.Concat(additionalCautionPatterns ?? Array.Empty<string>());
        _highConfidence = highConf.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
        _caution = caution.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
    }

    /// <inheritdoc />
    public string Id => "dotnetagents.safety-counter-agent";

    /// <inheritdoc />
    public string DisplayName => "Safety Counter-Agent";

    /// <inheritdoc />
    public Task<CounterAgentVerdict> ReviewAsync(
        CounterAgentActionProposal proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();

        // Materialize the search corpus from action name + serializable input snippet.
        var corpus = BuildSearchCorpus(proposal);
        if (string.IsNullOrEmpty(corpus))
        {
            return Task.FromResult(CounterAgentVerdict.Approve(Id));
        }

        var blockHits = _highConfidence
            .Select(p => p.Match(corpus))
            .Where(m => m.Success)
            .Select(m => m.Value)
            .ToArray();

        if (blockHits.Length > 0)
        {
            var reasons = blockHits.Select(h => $"Matched dangerous pattern: '{h}'").ToArray();
            return Task.FromResult(CounterAgentVerdict.Block(
                Id,
                reasons,
                CounterAgentSeverity.Critical,
                metadata: new Dictionary<string, object>
                {
                    ["matched_pattern_count"] = blockHits.Length,
                    ["match_kind"] = "high_confidence",
                }));
        }

        var concernHits = _caution
            .Select(p => p.Match(corpus))
            .Where(m => m.Success)
            .Select(m => m.Value)
            .ToArray();

        if (concernHits.Length > 0)
        {
            var reasons = concernHits.Select(h => $"Matched caution pattern: '{h}'").ToArray();
            return Task.FromResult(CounterAgentVerdict.Concern(
                Id,
                reasons,
                CounterAgentSeverity.Major,
                metadata: new Dictionary<string, object>
                {
                    ["matched_pattern_count"] = concernHits.Length,
                    ["match_kind"] = "caution",
                }));
        }

        return Task.FromResult(CounterAgentVerdict.Approve(Id));
    }

    private static string BuildSearchCorpus(CounterAgentActionProposal proposal)
    {
        var parts = new List<string> { proposal.ActionName };
        if (proposal.Input is string s) parts.Add(s);
        else if (proposal.Input is not null)
        {
            try
            {
                parts.Add(System.Text.Json.JsonSerializer.Serialize(proposal.Input));
            }
            catch
            {
                parts.Add(proposal.Input.ToString() ?? string.Empty);
            }
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}
