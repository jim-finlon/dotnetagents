namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Story e399bfce (SUC Projection P1 follow-up). Idempotent marker-bounded section graft —
/// the disk-side counterpart to <see cref="SkillProjectionMode.AppendSection"/>. Pure helper:
/// reads the target file (or treats it as empty when missing), replaces the existing section
/// between the marker pair (or appends it), and writes back via <see cref="AtomicFileWriter"/>.
/// </summary>
/// <remarks>
/// Marker convention matches <see cref="AgentsMdProjector.ApplyGraft(string, string, string)"/>:
/// <c>&lt;!-- dna-skill:&lt;markerKey&gt;:begin --&gt;</c> … <c>&lt;!-- dna-skill:&lt;markerKey&gt;:end --&gt;</c>.
/// The same convention is reused for the Copilot global-instructions graft surface so the
/// markers survive in HTML-comment form across Markdown + Copilot's own format.
/// </remarks>
public static class MarkerBoundedSectionWriter
{
    /// <summary>
    /// Pure string-in/string-out section graft. Replaces (or appends) the section between
    /// <c>&lt;!-- dna-skill:&lt;markerKey&gt;:begin --&gt;</c> markers in
    /// <paramref name="existingFileContent"/>. Idempotent: feeding the result back produces the
    /// same string. Operator-injected content outside the marker pair is preserved verbatim.
    /// </summary>
    /// <param name="existingFileContent">Current file content; empty string when the file does not exist.</param>
    /// <param name="markerKey">
    /// Stable per-section key — typically the skill id — used as <c>dna-skill:&lt;markerKey&gt;</c>
    /// in both marker tags. Must not be null or whitespace.
    /// </param>
    /// <param name="newSection">Section body to write between the markers (no markers required in the input).</param>
    /// <returns>The full target-file content with the section grafted in place.</returns>
    public static string ApplySection(string existingFileContent, string markerKey, string newSection)
    {
        ArgumentNullException.ThrowIfNull(existingFileContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(markerKey);
        ArgumentNullException.ThrowIfNull(newSection);

        var beginMarker = $"<!-- dna-skill:{markerKey}:begin -->";
        var endMarker = $"<!-- dna-skill:{markerKey}:end -->";
        var sectionBlock = $"{beginMarker}\n{newSection.TrimEnd('\n')}\n{endMarker}";

        var beginIdx = existingFileContent.IndexOf(beginMarker, StringComparison.Ordinal);
        var endIdx = existingFileContent.IndexOf(endMarker, StringComparison.Ordinal);

        if (beginIdx >= 0 && endIdx > beginIdx)
        {
            var prefix = existingFileContent[..beginIdx];
            var afterEnd = endIdx + endMarker.Length;
            var suffix = afterEnd < existingFileContent.Length ? existingFileContent[afterEnd..] : string.Empty;
            if (prefix.Length > 0 && !prefix.EndsWith('\n')) prefix += "\n";
            return prefix + sectionBlock + suffix;
        }

        if (existingFileContent.Length == 0)
            return sectionBlock + "\n";

        var sep = existingFileContent.EndsWith("\n\n") ? string.Empty
                : existingFileContent.EndsWith('\n') ? "\n"
                : "\n\n";
        return existingFileContent + sep + sectionBlock + "\n";
    }

    /// <summary>
    /// Read the target file (treat missing as empty), apply the section via
    /// <see cref="ApplySection"/>, and write back via <see cref="AtomicFileWriter"/>.
    /// Idempotent — repeat calls with identical inputs return
    /// <see cref="AtomicWriteOutcome.NoChange"/> without touching disk.
    /// </summary>
    public static async Task<AtomicWriteOutcome> WriteAsync(
        string targetFullPath,
        string markerKey,
        string newSection,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFullPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(markerKey);
        ArgumentNullException.ThrowIfNull(newSection);

        var existing = File.Exists(targetFullPath)
            ? await File.ReadAllTextAsync(targetFullPath, cancellationToken).ConfigureAwait(false)
            : string.Empty;

        var grafted = ApplySection(existing, markerKey, newSection);
        return await AtomicFileWriter.WriteAsync(targetFullPath, grafted, cancellationToken).ConfigureAwait(false);
    }
}
