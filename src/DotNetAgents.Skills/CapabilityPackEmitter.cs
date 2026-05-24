// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Skills;

/// <summary>
/// Emits <c>dna.skill.capability-pack.v1</c> packs (schema:
/// <c>docs/schemas/dna.skill.capability-pack.v1.schema.json</c>) from a set of canonical skill
/// manifests and the per-client projection outcomes reported by the projection orchestrator.
/// </summary>
/// <remarks>
/// <para>
/// SUC-08 deliverable. The emitter is deterministic: the same input always yields the same
/// serialised pack (JSON property order from the typed model, indent fixed at two spaces). Each
/// <c>contents[]</c> entry carries an <c>sha256:</c> checksum over the canonical SKILL.md body so
/// downstream consumers can verify the registry-served bytes match what was scored. Pack signing
/// (<c>provenance.signedBy[]</c>) is tracked as a follow-up — it needs CredentialsAgent identity
/// card integration and is intentionally out of scope here.
/// </para>
/// <para>
/// <see cref="ChannelManifestWriter"/> consumes the emitted packs and groups them into per-channel
/// JSON files (internal-only / experimental / stable / deprecated) for catalog publication.
/// </para>
/// </remarks>
public static class CapabilityPackEmitter
{
    /// <summary>
    /// Emit a single capability pack from <paramref name="contents"/> and per-client compatibility
    /// records reported by the orchestrator.
    /// </summary>
    /// <param name="packKind">Pack kind (skill | hook | prompt | policy | eval-pack | composite).</param>
    /// <param name="contents">One entry per canonical skill in the pack.</param>
    /// <param name="clientCompatibility">One entry per (client, emitter) pair the projection orchestrator ran successfully.</param>
    /// <param name="rolloutChannel">Channel for catalog routing.</param>
    /// <param name="approvalState">Lifecycle state.</param>
    /// <param name="scoring">Optional scoring snapshot from the registry.</param>
    /// <param name="provenance">Optional provenance (sourceRepoRef, extractedFromLessonRef). Signing is a follow-up.</param>
    public static CapabilityPack Emit(
        CapabilityPackKind packKind,
        IReadOnlyList<CapabilityPackContent> contents,
        IReadOnlyList<CapabilityPackClientCompatibility> clientCompatibility,
        CapabilityPackChannel rolloutChannel,
        CapabilityPackApprovalState approvalState,
        CapabilityPackScoring? scoring = null,
        CapabilityPackProvenance? provenance = null)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(clientCompatibility);
        if (contents.Count == 0)
        {
            throw new ArgumentException("Capability pack must have at least one contents entry.", nameof(contents));
        }
        if (clientCompatibility.Count == 0)
        {
            throw new ArgumentException("Capability pack must have at least one clientCompatibility entry.", nameof(clientCompatibility));
        }
        if (packKind == CapabilityPackKind.Composite && contents.Count < 2)
        {
            throw new ArgumentException("Composite packs must contain at least two contents entries (schema §3.2).", nameof(contents));
        }
        foreach (var entry in clientCompatibility)
        {
            if (entry.Verified && entry.VerifiedAtUtc is null)
            {
                throw new ArgumentException(
                    $"clientCompatibility entry for '{entry.Client}/{entry.Emitter}' is verified=true but missing verifiedAtUtc (schema §6.2).",
                    nameof(clientCompatibility));
            }
        }

        return new CapabilityPack(
            SchemaVersion: SchemaVersionConst,
            PackKind: packKind,
            Contents: contents,
            ClientCompatibility: clientCompatibility,
            RolloutChannel: rolloutChannel,
            ApprovalState: approvalState,
            Scoring: scoring,
            Provenance: provenance);
    }

    /// <summary>
    /// Build a <see cref="CapabilityPackContent"/> entry for a canonical skill body. The checksum
    /// is computed over the UTF-8 bytes of <paramref name="canonicalSkillBody"/> as
    /// <c>sha256:&lt;hex&gt;</c>.
    /// </summary>
    public static CapabilityPackContent BuildContent(
        string contentId,
        CapabilityPackContentKind contentKind,
        string version,
        string canonicalSkillBody,
        string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(canonicalSkillBody);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        var checksum = ComputeChecksum(canonicalSkillBody);
        return new CapabilityPackContent(contentId, contentKind, version, checksum, location);
    }

    /// <summary>Compute the canonical <c>sha256:&lt;hex&gt;</c> checksum for a string body.</summary>
    public static string ComputeChecksum(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var bytes = Encoding.UTF8.GetBytes(body);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder("sha256:", 7 + hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    /// <summary>Serialise <paramref name="pack"/> to the canonical JSON shape with stable formatting.</summary>
    public static string Serialize(CapabilityPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        return JsonSerializer.Serialize(pack, JsonOpts) + "\n";
    }

    internal const string SchemaVersionConst = "dna.skill.capability-pack.v1";

    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter<CapabilityPackKind>(JsonNamingPolicy.KebabCaseLower),
            new JsonStringEnumConverter<CapabilityPackContentKind>(JsonNamingPolicy.KebabCaseLower),
            new JsonStringEnumConverter<CapabilityPackChannel>(JsonNamingPolicy.KebabCaseLower),
            new JsonStringEnumConverter<CapabilityPackApprovalState>(JsonNamingPolicy.CamelCase),
        },
    };
}
