using System.Text.RegularExpressions;
using DotNetAgents.Abstractions.Hooks;

namespace DotNetAgents.Core.Hooks;

/// <summary>
/// PostLlmCall + PostToolUse hook that scrubs known secret-shaped patterns from outputs
/// before downstream code (logging, evidence storage, dashboards) sees them. Pattern list
/// is operator-extensible; defaults catch GitHub PATs, OpenAI keys, AWS access keys,
/// Chat tokens, and JWT-shaped strings.
/// </summary>
/// <remarks>
/// Defense-in-depth: this hook does NOT replace transcript-time scrubbing in transcript
/// pipelines (per ContextIntent v1's <c>provenance.scrubbed_patterns</c> contract). It
/// catches the cases where an agent re-emits a secret in its output, possibly because it
/// was provided one in tool input.
/// </remarks>
public sealed class RedactSecretsHook : IAgentHook
{
    /// <summary>Default secret-shaped patterns. Operators can extend at construction.</summary>
    public static readonly IReadOnlyList<string> DefaultSecretPatterns = new[]
    {
        @"\bghp_[A-Za-z0-9]{36,40}\b",                 // GitHub PAT (classic)
        @"\bgithub_pat_[A-Za-z0-9_]{82,}\b",           // GitHub fine-grained PAT
        @"\bsk-[A-Za-z0-9]{32,}\b",                    // OpenAI keys / Anthropic-style
        @"\bxox[baprs]-[A-Za-z0-9-]{10,}\b",           // Chat tokens
        @"\bAKIA[A-Z0-9]{16}\b",                       // AWS access key id
        @"\beyJ[A-Za-z0-9_=-]{16,}\.[A-Za-z0-9_=-]{16,}\.[A-Za-z0-9_=-]{16,}\b", // JWT-shaped 3-segment
    };

    private static readonly IReadOnlySet<HookCheckpoint> _checkpoints =
        new HashSet<HookCheckpoint> { HookCheckpoint.PostLlmCall, HookCheckpoint.PostToolUse };

    private readonly IReadOnlyList<Regex> _patterns;
    private readonly string _redactionPlaceholder;

    public RedactSecretsHook(
        IEnumerable<string>? additionalPatterns = null,
        string redactionPlaceholder = "[REDACTED]")
    {
        var combined = DefaultSecretPatterns.Concat(additionalPatterns ?? Array.Empty<string>());
        _patterns = combined.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToArray();
        _redactionPlaceholder = redactionPlaceholder;
    }

    public string Id => "dotnetagents.redact-secrets-hook";
    public string DisplayName => "Redact Secrets Hook";
    public IReadOnlySet<HookCheckpoint> SubscribedCheckpoints => _checkpoints;
    public int Priority => 5; // Run early so downstream hooks see redacted text

    public Task<HookDecision> EvaluateAsync(
        AgentHookContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Payload is not string payloadString)
        {
            // Try to serialize the payload to scan it
            try
            {
                payloadString = System.Text.Json.JsonSerializer.Serialize(context.Payload);
            }
            catch
            {
                return Task.FromResult<HookDecision>(HookDecision.Allow);
            }
        }

        var matchedLabels = new List<string>();
        var redacted = payloadString;
        for (var i = 0; i < _patterns.Count; i++)
        {
            var pattern = _patterns[i];
            if (pattern.IsMatch(redacted))
            {
                matchedLabels.Add($"pattern[{i}]");
                redacted = pattern.Replace(redacted, _redactionPlaceholder);
            }
        }

        if (matchedLabels.Count == 0)
        {
            return Task.FromResult<HookDecision>(HookDecision.Allow);
        }

        return Task.FromResult<HookDecision>(HookDecision.RedactedTo(
            redacted,
            new[] { $"Redacted {matchedLabels.Count} secret-shaped substring(s): {string.Join(", ", matchedLabels)}" },
            Id));
    }
}
