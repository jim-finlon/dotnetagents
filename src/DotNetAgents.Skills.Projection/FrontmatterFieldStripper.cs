// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Shared helper for removing a top-level YAML key (and its block-scalar continuation lines)
/// from a frontmatter block. Used by projectors that strip Claude-Code superset fields when
/// targeting non-Claude surfaces.
/// </summary>
/// <remarks>
/// Returns the original instance via reference equality when the field is absent so callers can
/// detect a no-op without an extra equality check.
/// </remarks>
internal static class FrontmatterFieldStripper
{
    public static string Strip(string frontmatter, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(frontmatter);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        var lines = frontmatter.Split('\n');
        var output = new List<string>(lines.Length);
        var i = 0;
        var stripped = false;
        while (i < lines.Length)
        {
            var line = lines[i];
            if (IsTopLevelKeyMatch(line, fieldName))
            {
                stripped = true;
                i++;
                while (i < lines.Length && IsBlockScalarContinuation(lines[i]))
                {
                    i++;
                }
                continue;
            }
            output.Add(line);
            i++;
        }

        return stripped ? string.Join('\n', output) : frontmatter;
    }

    private static bool IsTopLevelKeyMatch(string line, string fieldName)
    {
        var trimmedStart = 0;
        while (trimmedStart < line.Length && line[trimmedStart] == ' ') trimmedStart++;
        if (trimmedStart != 0) return false;
        if (line.Length <= fieldName.Length) return false;
        if (!line.AsSpan(0, fieldName.Length).SequenceEqual(fieldName.AsSpan())) return false;
        return line[fieldName.Length] == ':';
    }

    private static bool IsBlockScalarContinuation(string line)
    {
        if (line.Length == 0) return false;
        return line[0] == ' ' || line[0] == '\t' || line[0] == '-';
    }
}
