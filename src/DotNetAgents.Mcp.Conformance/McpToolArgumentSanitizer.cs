// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetAgents.Mcp.Conformance;

/// <summary>
/// Redacts likely secrets from MCP tool arguments before logging or telemetry (defense in depth — callers must still avoid logging raw secrets).
/// </summary>
public static class McpToolArgumentSanitizer
{
    private const string Redacted = "[REDACTED]";

    private static readonly Regex JwtLike = new(
        @"\beyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\b",
        RegexOptions.Compiled);

    private static readonly Regex HexKeyLike = new(
        @"\b[0-9a-fA-F]{32,}\b",
        RegexOptions.Compiled);

    /// <summary>
    /// Keys whose values are always redacted (case-insensitive substring match on argument name).
    /// </summary>
    public static readonly IReadOnlyCollection<string> SensitiveKeySubstrings =
        new[]
        {
            "password", "passwd", "secret", "token", "api_key", "apikey", "authorization",
            "bearer", "credential", "private_key", "client_secret"
        };

    /// <summary>
    /// Returns a new dictionary safe for structured logs: string values matching secret patterns or sensitive keys are replaced.
    /// </summary>
    public static Dictionary<string, object> SanitizeForLogging(IReadOnlyDictionary<string, object> arguments)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in arguments)
        {
            result[kv.Key] = SanitizeValue(kv.Key, kv.Value);
        }

        return result;
    }

    /// <summary>
    /// Redacts JWT-like fragments, long hex blobs, and common secret key names in a single string (e.g. serialized JSON).
    /// </summary>
    public static string SanitizeString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        var s = JwtLike.Replace(value, Redacted);
        s = HexKeyLike.Replace(s, m => m.Value.Length >= 32 ? Redacted : m.Value);
        return s;
    }

    private static object SanitizeValue(string key, object value)
    {
        if (IsSensitiveKey(key))
            return Redacted;

        return value switch
        {
            string str => SanitizeString(str),
            JsonElement je when je.ValueKind == JsonValueKind.String => SanitizeString(je.GetString() ?? ""),
            JsonElement je => je.Clone(),
            _ => value
        };
    }

    private static bool IsSensitiveKey(string key)
    {
        var lower = key.ToLowerInvariant();
        foreach (var part in SensitiveKeySubstrings)
        {
            if (lower.Contains(part, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
