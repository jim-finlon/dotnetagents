using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Skills;

/// <summary>
/// Groups <see cref="CapabilityPack"/> instances by <see cref="CapabilityPackChannel"/> and
/// writes a per-channel manifest JSON file. Used as the catalog-publication step that comes
/// after <see cref="CapabilityPackEmitter"/>: every pack the orchestrator emits is added to its
/// channel's manifest, and the manifest becomes the catalog's authoritative pack index.
/// </summary>
/// <remarks>
/// <para>
/// SUC-08 deliverable. Manifest shape (out-of-schema today; the
/// <c>dna.skill.capability-pack.v1</c> schema validates a single pack, not a manifest of packs):
/// </para>
/// <code language="json">
/// {
///   "schemaVersion": "dna.skill.channel-manifest.v1",
///   "channel": "stable",
///   "packs": [ { ... pack document ... }, { ... } ]
/// }
/// </code>
/// <para>
/// The writer is deterministic: same input → same bytes. Packs are sorted by
/// <c>packKind</c>, then by the lexicographically smallest <c>contents[].contentId</c> so the
/// emitted manifest is stable across regen passes.
/// </para>
/// </remarks>
public static class ChannelManifestWriter
{
    internal const string ManifestSchemaVersion = "dna.skill.channel-manifest.v1";

    /// <summary>Group <paramref name="packs"/> by rolloutChannel and build one manifest per channel.</summary>
    public static IReadOnlyList<ChannelManifest> BuildManifests(IEnumerable<CapabilityPack> packs)
    {
        ArgumentNullException.ThrowIfNull(packs);
        var byChannel = packs
            .GroupBy(p => p.RolloutChannel)
            .OrderBy(g => g.Key);

        var manifests = new List<ChannelManifest>();
        foreach (var group in byChannel)
        {
            var ordered = group
                .OrderBy(p => p.PackKind)
                .ThenBy(p => p.Contents.Select(c => c.ContentId).OrderBy(s => s, StringComparer.Ordinal).First(), StringComparer.Ordinal)
                .ToList();
            manifests.Add(new ChannelManifest(
                SchemaVersion: ManifestSchemaVersion,
                Channel: group.Key,
                Packs: ordered));
        }
        return manifests;
    }

    /// <summary>Serialise <paramref name="manifest"/> to canonical JSON with a trailing newline.</summary>
    public static string Serialize(ChannelManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return JsonSerializer.Serialize(manifest, CapabilityPackEmitter.JsonOpts) + "\n";
    }

    /// <summary>
    /// Convenience helper: write every channel manifest produced from <paramref name="packs"/>
    /// to <paramref name="outputDirectory"/> as <c>&lt;channel&gt;.json</c>. Returns the per-channel
    /// (path, written-bytes) tuples so callers can log what changed.
    /// </summary>
    public static IReadOnlyList<(string Path, int Bytes)> WriteAll(
        IEnumerable<CapabilityPack> packs,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(packs);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var results = new List<(string, int)>();
        foreach (var manifest in BuildManifests(packs))
        {
            var channelName = JsonNamingPolicy.KebabCaseLower.ConvertName(manifest.Channel.ToString());
            var filePath = Path.Combine(outputDirectory, $"{channelName}.json");
            var bytes = Serialize(manifest);
            File.WriteAllText(filePath, bytes);
            results.Add((filePath, bytes.Length));
        }
        return results;
    }
}

/// <summary>Channel manifest document (one per <see cref="CapabilityPackChannel"/>).</summary>
public sealed record ChannelManifest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("channel")] CapabilityPackChannel Channel,
    [property: JsonPropertyName("packs")] IReadOnlyList<CapabilityPack> Packs);
