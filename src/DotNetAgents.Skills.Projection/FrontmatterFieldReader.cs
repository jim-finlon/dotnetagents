namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Helper for reading a single top-level field's scalar value from raw YAML frontmatter.
/// Handles the three forms canonical SKILL.md frontmatter actually uses today: plain inline
/// scalar, double-quoted inline scalar, and folded block scalar (<c>&gt;-</c> or <c>&gt;</c>).
/// </summary>
/// <remarks>Promoted from <c>internal</c> to <c>public</c> for story 55c71906 so the
/// channel-manifest orchestrator script can reuse the same version/field extraction logic the
/// projectors use, without re-implementing frontmatter parsing inline.</remarks>
/// <remarks>
/// <para>
/// Phase 1 deliberately avoids pulling in a full YAML parser — full structured access lands when
/// the registry HTTP API (SUC-02) consumes the <c>dna.skill.v1</c> schema. Until then, projectors
/// that need a single field (description, posture, …) read it through this helper. Behaviour for
/// each form follows YAML 1.2 well enough for human-authored DNA SKILL.md files:
/// </para>
/// <list type="bullet">
///   <item><c>field: value</c> → <c>value</c></item>
///   <item><c>field: "value"</c> → <c>value</c> (outer quotes stripped)</item>
///   <item><c>field: '\''value'\''</c> → <c>value</c> (outer quotes stripped)</item>
///   <item>
///     <c>field: &gt;-</c> followed by indented continuation → continuation lines joined with
///     single spaces; trailing newline stripped (<c>-</c> chomping). Indicator <c>&gt;</c>
///     without <c>-</c> behaves the same here (single-trailing-newline normalised away by
///     the caller's downstream consumers).
///   </item>
/// </list>
/// <para>Literal scalars (<c>|</c>, <c>|-</c>), explicit indentation indicators (<c>&gt;2</c>),
/// and multi-document streams are out of scope; the SUC-02 structured parser will own those.</para>
/// </remarks>
public static class FrontmatterFieldReader
{
    /// <summary>
    /// Read the top-level <paramref name="fieldName"/>'s value from <paramref name="frontmatter"/>.
    /// Returns <c>string.Empty</c> when the field is absent.
    /// </summary>
    public static string Read(string frontmatter, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        var lines = frontmatter.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!TryMatchTopLevelKey(lines[i], fieldName, out var rest))
            {
                continue;
            }

            rest = rest.TrimStart();

            // Folded block scalar: ">-" or ">". Collect continuation lines that are indented
            // (any leading whitespace), join with single spaces, strip wrapping whitespace.
            if (rest.StartsWith(">", StringComparison.Ordinal))
            {
                return ReadFoldedScalar(lines, i + 1);
            }

            // Inline scalar (plain or quoted). Strip optional trailing comment and outer quotes.
            return ReadInlineScalar(rest);
        }
        return string.Empty;
    }

    /// <summary>
    /// Read one nested field's scalar value from a top-level block mapping. Used today for
    /// the <c>invocation</c> block (e.g. <c>invocation.modelInvokable</c>). Returns
    /// <c>string.Empty</c> when the parent block is absent or the nested key is missing.
    /// </summary>
    /// <remarks>
    /// Supported nested shape:
    /// <code language="yaml">
    /// invocation:
    ///   modelInvokable: false
    ///   allowImplicitInvocation: false
    /// </code>
    /// Inline flow mappings (<c>invocation: { modelInvokable: false }</c>) are NOT supported in
    /// this slice; if a canonical skill needs them, expand the helper rather than rolling a one-off
    /// regex per call site.
    /// </remarks>
    public static string ReadNested(string frontmatter, string parentKey, string childKey)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(childKey);

        var lines = frontmatter.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!TryMatchTopLevelKey(lines[i], parentKey, out var rest))
            {
                continue;
            }
            rest = rest.TrimStart();
            // Block mapping form: the rest of the line is empty (or starts a comment) and
            // continuation lines carry the nested keys. Inline flow form is unsupported here.
            if (!string.IsNullOrEmpty(rest) && !rest.StartsWith("#", StringComparison.Ordinal))
            {
                return string.Empty;
            }
            for (var j = i + 1; j < lines.Length; j++)
            {
                var line = lines[j];
                if (line.Length == 0) continue;
                // Non-indented line terminates the block.
                if (!char.IsWhiteSpace(line[0])) break;
                var trimmed = line.TrimStart();
                if (!trimmed.StartsWith(childKey, StringComparison.Ordinal)) continue;
                if (trimmed.Length <= childKey.Length || trimmed[childKey.Length] != ':') continue;
                return ReadInlineScalar(trimmed[(childKey.Length + 1)..]);
            }
            return string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Read a top-level field as a YAML sequence. Supports the two human-authored forms used in
    /// DNA SKILL.md frontmatter: inline flow sequence (<c>paths: [a, b]</c>) and block sequence
    /// (<c>paths:</c> on one line, then <c>  - item</c> per element). Returns an empty list when
    /// the field is absent or empty.
    /// </summary>
    public static IReadOnlyList<string> ReadSequence(string frontmatter, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        var lines = frontmatter.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!TryMatchTopLevelKey(lines[i], fieldName, out var rest))
            {
                continue;
            }

            rest = rest.TrimStart();
            if (rest.StartsWith("[", StringComparison.Ordinal))
            {
                return ParseFlowSequence(rest);
            }

            return ReadBlockSequence(lines, i + 1);
        }
        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ParseFlowSequence(string text)
    {
        // Strip optional trailing comment, then the enclosing brackets.
        var hashIdx = text.IndexOf('#');
        if (hashIdx >= 0) text = text[..hashIdx];
        text = text.Trim();
        if (text.Length < 2 || text[0] != '[' || text[^1] != ']')
        {
            return Array.Empty<string>();
        }
        var inner = text[1..^1].Trim();
        if (inner.Length == 0) return Array.Empty<string>();
        var parts = inner.Split(',');
        var items = new List<string>(parts.Length);
        foreach (var raw in parts)
        {
            var item = raw.Trim();
            if (item.Length >= 2 && ((item[0] == '"' && item[^1] == '"') || (item[0] == '\'' && item[^1] == '\'')))
            {
                item = item[1..^1];
            }
            if (item.Length > 0) items.Add(item);
        }
        return items;
    }

    private static IReadOnlyList<string> ReadBlockSequence(string[] lines, int startIndex)
    {
        var items = new List<string>();
        for (var i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length == 0) continue;
            // A non-empty non-whitespace-leading line terminates the sequence (next top-level key).
            if (!char.IsWhiteSpace(line[0]) && line[0] != '-') break;
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("-", StringComparison.Ordinal)) break;
            var item = trimmed[1..].Trim();
            if (item.Length >= 2 && ((item[0] == '"' && item[^1] == '"') || (item[0] == '\'' && item[^1] == '\'')))
            {
                item = item[1..^1];
            }
            if (item.Length > 0) items.Add(item);
        }
        return items;
    }

    private static bool TryMatchTopLevelKey(string line, string fieldName, out string rest)
    {
        rest = string.Empty;
        if (line.Length <= fieldName.Length) return false;
        if (!line.AsSpan(0, fieldName.Length).SequenceEqual(fieldName.AsSpan())) return false;
        if (line[fieldName.Length] != ':') return false;
        // Top-level only: column-0 anchor (no leading whitespace before fieldName).
        // The check above already requires the key at column 0 since we sliced from index 0.
        rest = line[(fieldName.Length + 1)..];
        return true;
    }

    private static string ReadInlineScalar(string raw)
    {
        // Trim a trailing YAML comment (`# ...`) and surrounding whitespace.
        var hashIdx = raw.IndexOf('#');
        if (hashIdx >= 0)
        {
            raw = raw[..hashIdx];
        }
        raw = raw.Trim();
        if (raw.Length >= 2)
        {
            if (raw[0] == '"' && raw[^1] == '"')
            {
                return raw[1..^1];
            }
            if (raw[0] == '\'' && raw[^1] == '\'')
            {
                return raw[1..^1];
            }
        }
        return raw;
    }

    private static string ReadFoldedScalar(string[] lines, int startIndex)
    {
        // Folded block scalar: indented continuation lines are joined with a single space; blank
        // lines become a literal newline (per YAML 1.2 folded semantics) — but DNA skills haven't
        // used blank lines inside descriptions, so we treat them as a paragraph break (newline).
        var parts = new List<string>();
        for (var i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            // A line that is non-empty but not indented terminates the scalar (next top-level key).
            if (line.Length > 0 && !char.IsWhiteSpace(line[0])) break;
            var trimmed = line.Trim();
            parts.Add(trimmed);
        }

        // Build folded output: blank-line → paragraph break (\n), otherwise join with space.
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (string.IsNullOrEmpty(part))
            {
                if (sb.Length > 0 && sb[^1] != '\n')
                {
                    sb.Append('\n');
                }
                continue;
            }
            if (sb.Length > 0 && sb[^1] != '\n')
            {
                sb.Append(' ');
            }
            sb.Append(part);
        }
        return sb.ToString().TrimEnd('\n', ' ');
    }
}
