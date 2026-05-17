using System.Text;

namespace DotNetAgents.StructuredOutput;

/// <summary>
/// Helpers for tolerantly extracting a strict JSON object from a model response that may carry
/// reasoning-trace prefixes (Qwen3 / DeepSeek-R1 <c>&lt;think&gt;…&lt;/think&gt;</c>,
/// generic <c>&lt;reasoning&gt;…&lt;/reasoning&gt;</c>) and/or markdown code fences
/// (<c>```json … ```</c>). Used by every DNA pipeline that asks an LLM gateway for structured
/// output and parses the result with a strict deserializer; replaces direct <c>JsonDocument.Parse</c>
/// calls that would silently throw and fall back to deterministic stubs.
/// </summary>
/// <remarks>
/// Story 6aae2a06 surfaced the bug class from the live Frey vLLM smoke (Qwen3-32B-4bit) which
/// prefixed every assistant response with a <c>&lt;think&gt;…&lt;/think&gt;</c> block, breaking
/// the strict <c>JsonDocument.Parse</c> path in GEPA's reflector + variant generator. Story
/// <c>9efbbca7</c> promoted the helper from <c>PromptSpecialistAgent.Application.Services.Gepa</c>
/// to <c>DotNetAgents.StructuredOutput</c> so MediaProduction + Education + future consumers can
/// reuse it without taking a cross-submodule project reference into PromptSpecialist.
/// </remarks>
public static class LlmJsonResponseExtractor
{
    private static readonly string[] ReasoningTags = { "think", "reasoning", "thought", "scratchpad" };

    /// <summary>
    /// Returns the first balanced JSON object found in <paramref name="response"/> after
    /// stripping reasoning-trace blocks and markdown fences. Returns null when no balanced
    /// object can be located. Never throws.
    /// </summary>
    public static string? ExtractFirstJsonObject(string? response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;

        var stripped = StripReasoningBlocks(response!);
        stripped = StripMarkdownFences(stripped);
        return ExtractBalancedObject(stripped);
    }

    /// <summary>
    /// Returns the first balanced JSON value (object <c>{…}</c> or array <c>[…]</c>) found
    /// in <paramref name="response"/> after stripping reasoning blocks and markdown fences.
    /// Use this when the LLM is asked for an array (e.g. a list of misconceptions, items, or
    /// labels) rather than an object. Returns null when no balanced value is present.
    /// </summary>
    public static string? ExtractFirstJsonValue(string? response)
    {
        if (string.IsNullOrWhiteSpace(response)) return null;

        var stripped = StripReasoningBlocks(response!);
        stripped = StripMarkdownFences(stripped);

        // Whichever opener appears first wins.
        var firstObj = stripped.IndexOf('{');
        var firstArr = stripped.IndexOf('[');
        if (firstObj < 0 && firstArr < 0) return null;
        if (firstArr < 0 || (firstObj >= 0 && firstObj < firstArr))
            return ExtractBalancedObject(stripped);
        return ExtractBalancedArray(stripped);
    }

    /// <summary>Removes <c>&lt;think&gt;…&lt;/think&gt;</c> and similar reasoning-tag pairs (case-insensitive).</summary>
    public static string StripReasoningBlocks(string response)
    {
        if (string.IsNullOrEmpty(response)) return response;

        var sb = new StringBuilder(response.Length);
        var i = 0;
        while (i < response.Length)
        {
            var openTag = MatchOpeningReasoningTag(response, i);
            if (openTag is null)
            {
                sb.Append(response[i]);
                i++;
                continue;
            }

            // Skip through the matching close tag (or to end of string if no close tag found).
            var closeIndex = FindMatchingCloseTag(response, i + openTag.Length, openTag.Tag);
            if (closeIndex < 0)
            {
                // Unterminated reasoning block — drop the rest of the response. Reasoning models
                // sometimes emit truncated <think> sections; treating them as no-content is safer
                // than re-introducing them into the parse stream.
                break;
            }
            i = closeIndex;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Strips a leading markdown fence wrapper around the entire payload — handles both
    /// triple-backtick and triple-tilde fences with optional language tag (json, JSON, plain, …).
    /// </summary>
    public static string StripMarkdownFences(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return response;

        var trimmed = response.TrimStart();
        var fence = trimmed.StartsWith("```", StringComparison.Ordinal) ? "```"
                  : trimmed.StartsWith("~~~", StringComparison.Ordinal) ? "~~~"
                  : null;
        if (fence is null) return response;

        // Skip past the fence + optional language tag + newline.
        var afterFence = trimmed[fence.Length..];
        var newline = afterFence.IndexOf('\n');
        if (newline < 0) return response; // malformed fence — return unchanged so the parser can fail loudly
        var body = afterFence[(newline + 1)..];

        var closeIndex = body.LastIndexOf(fence, StringComparison.Ordinal);
        if (closeIndex < 0) return body; // no closing fence — keep the body
        return body[..closeIndex];
    }

    private static string? ExtractBalancedObject(string text) => ExtractBalanced(text, '{', '}');

    private static string? ExtractBalancedArray(string text) => ExtractBalanced(text, '[', ']');

    private static string? ExtractBalanced(string text, char open, char close)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var firstOpen = text.IndexOf(open);
        if (firstOpen < 0) return null;

        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = firstOpen; i < text.Length; i++)
        {
            var ch = text[i];
            if (escape) { escape = false; continue; }
            if (ch == '\\' && inString) { escape = true; continue; }
            if (ch == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (ch == open) depth++;
            else if (ch == close)
            {
                depth--;
                if (depth == 0) return text.Substring(firstOpen, i - firstOpen + 1);
            }
        }
        return null;
    }

    private sealed record TagMatch(string Tag, int Length);

    private static TagMatch? MatchOpeningReasoningTag(string text, int start)
    {
        if (start >= text.Length || text[start] != '<') return null;
        foreach (var tag in ReasoningTags)
        {
            // Match `<tag>` or `<tag attr=...>` (case-insensitive). Length covers `<` + tag + `>`.
            var minLength = tag.Length + 2; // <tag>
            if (start + minLength > text.Length) continue;

            // Compare the tag name case-insensitively.
            var span = text.AsSpan(start + 1, tag.Length);
            if (!span.Equals(tag, StringComparison.OrdinalIgnoreCase)) continue;

            // After the tag name we expect either `>` (no attributes) or whitespace/`>` (with attrs).
            var afterTag = text[start + 1 + tag.Length];
            if (afterTag == '>') return new TagMatch(tag, minLength);
            if (afterTag == ' ' || afterTag == '\t')
            {
                var closeBracket = text.IndexOf('>', start + 1 + tag.Length);
                if (closeBracket > 0) return new TagMatch(tag, closeBracket - start + 1);
            }
        }
        return null;
    }

    private static int FindMatchingCloseTag(string text, int searchFrom, string tag)
    {
        var closeMarker = $"</{tag}";
        var idx = searchFrom;
        while (idx < text.Length)
        {
            idx = text.IndexOf(closeMarker, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return -1;
            var afterTag = idx + closeMarker.Length;
            if (afterTag < text.Length && (text[afterTag] == '>' || text[afterTag] == ' ' || text[afterTag] == '\t'))
            {
                var closeBracket = text.IndexOf('>', afterTag);
                if (closeBracket >= 0) return closeBracket + 1;
            }
            idx++;
        }
        return -1;
    }
}
