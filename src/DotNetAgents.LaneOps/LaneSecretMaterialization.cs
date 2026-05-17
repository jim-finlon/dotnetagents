using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetAgents.LaneOps;

/// <summary>
/// Contract for short-lived automated worker secret materialization and artifact redaction.
/// </summary>
public sealed record LaneSecretClaim(
    string ClaimId,
    string Category,
    string Name);

public sealed record MaterializedLaneSecret(
    string ClaimId,
    string Reference,
    string Value,
    DateTimeOffset ExpiresAtUtc);

public sealed record LaneSecretMaterialization(
    string LaneId,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyList<MaterializedLaneSecret> Secrets)
{
    public string MaterializationId { get; } = $"lanesecret_{Guid.NewGuid():N}";
}

public sealed class LaneSecretScope : IDisposable
{
    private readonly Dictionary<string, MaterializedLaneSecret> _secrets;
    private bool _disposed;

    public LaneSecretScope(LaneSecretMaterialization materialization, TimeProvider? timeProvider = null)
    {
        Materialization = materialization ?? throw new ArgumentNullException(nameof(materialization));
        TimeProvider = timeProvider ?? TimeProvider.System;
        _secrets = materialization.Secrets.ToDictionary(static secret => secret.ClaimId, StringComparer.Ordinal);
    }

    public LaneSecretMaterialization Materialization { get; }
    public TimeProvider TimeProvider { get; }

    public string GetRequiredValue(string claimId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (TimeProvider.GetUtcNow() >= Materialization.ExpiresAtUtc)
        {
            throw new InvalidOperationException($"Lane secret materialization '{Materialization.MaterializationId}' is expired.");
        }

        return _secrets.TryGetValue(claimId, out var secret)
            ? secret.Value
            : throw new KeyNotFoundException($"Lane secret claim '{claimId}' was not materialized.");
    }

    public void Dispose()
    {
        _disposed = true;
        _secrets.Clear();
    }
}

public sealed record ArtifactRedactionRule(string Id, string Pattern, string Replacement = "[REDACTED]");

public sealed class ArtifactRedactionPolicy
{
    public ArtifactRedactionPolicy(IEnumerable<ArtifactRedactionRule> rules)
    {
        Rules = rules?.ToArray() ?? throw new ArgumentNullException(nameof(rules));
        if (Rules.Count == 0)
        {
            throw new ArgumentException("At least one redaction rule is required.", nameof(rules));
        }
    }

    public IReadOnlyList<ArtifactRedactionRule> Rules { get; }

    public static ArtifactRedactionPolicy Default { get; } = new(
    [
        new("bearer-token", @"\bBearer\s+[A-Za-z0-9._~+/=-]{12,}"),
        new("api-key-assignment", @"(?i)\b(api[_-]?key|secret|token|password)\s*[:=]\s*[""']?[^""'\s,;]{8,}"),
        new("email", @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b"),
        new("phone", @"(?<!\d)(?:\+?1[\s.-]?)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}(?!\d)"),
        new("ssn", @"\b\d{3}-\d{2}-\d{4}\b"),
        new("canary-token", @"\bDNA_CANARY_SECRET_[A-Za-z0-9_-]+\b")
    ]);
}

public sealed class ArtifactRedactor
{
    private readonly IReadOnlyList<(ArtifactRedactionRule Rule, Regex Regex)> _rules;

    public ArtifactRedactor(ArtifactRedactionPolicy? policy = null)
    {
        _rules = (policy ?? ArtifactRedactionPolicy.Default).Rules
            .Select(static rule => (rule, new Regex(rule.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250))))
            .ToArray();
    }

    public RedactedArtifact RedactText(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var redacted = content;
        var matchedRules = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (rule, regex) in _rules)
        {
            if (!regex.IsMatch(redacted))
            {
                continue;
            }

            redacted = regex.Replace(redacted, rule.Replacement);
            matchedRules.Add(rule.Id);
        }

        return new RedactedArtifact(redacted, matchedRules.Order(StringComparer.Ordinal).ToArray());
    }

    public RedactedArtifact RedactNdjson(string ndjson)
    {
        ArgumentNullException.ThrowIfNull(ndjson);

        var lines = ndjson.Split('\n');
        var redactedLines = new string[lines.Length];
        var matchedRules = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                redactedLines[i] = line;
                continue;
            }

            var redacted = RedactJsonStringValues(line);
            foreach (var rule in redacted.MatchedRuleIds)
            {
                matchedRules.Add(rule);
            }

            redactedLines[i] = redacted.Content;
        }

        return new RedactedArtifact(string.Join('\n', redactedLines), matchedRules.Order(StringComparer.Ordinal).ToArray());
    }

    private RedactedArtifact RedactJsonStringValues(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                var matchedRules = new HashSet<string>(StringComparer.Ordinal);
                WriteRedactedElement(doc.RootElement, writer, matchedRules);
                writer.Flush();
                return new RedactedArtifact(System.Text.Encoding.UTF8.GetString(stream.ToArray()), matchedRules.Order(StringComparer.Ordinal).ToArray());
            }
        }
        catch (JsonException)
        {
            return RedactText(line);
        }
    }

    private void WriteRedactedElement(JsonElement element, Utf8JsonWriter writer, HashSet<string> matchedRules)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteRedactedElement(property.Value, writer, matchedRules);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteRedactedElement(item, writer, matchedRules);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                var redacted = RedactText(element.GetString() ?? string.Empty);
                foreach (var rule in redacted.MatchedRuleIds)
                {
                    matchedRules.Add(rule);
                }

                writer.WriteStringValue(redacted.Content);
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}

public sealed record RedactedArtifact(string Content, IReadOnlyList<string> MatchedRuleIds);
