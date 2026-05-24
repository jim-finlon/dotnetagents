// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Path-map entry from <c>config/skills-projection/agentskills-io-targets.json</c>. One entry per
/// signatory (Goose, OpenHands, Junie, Letta, Roo, Mux, Factory, Kiro, fast-agent, OpenCode,
/// Gemini-CLI, …) that should receive a generic agentskills.io-baseline SKILL.md projection.
/// </summary>
/// <param name="Client">
/// Stable agentskills.io signatory id (kebab-case). Used as the projector's
/// <see cref="ISkillProjector.ClientKind"/>.
/// </param>
/// <param name="RelativePath">
/// Path prefix relative to repo root (or actor home for user-scope) under which the projected
/// SKILL.md lands. The projector appends <c>/&lt;name&gt;/SKILL.md</c> at emit time.
/// </param>
/// <param name="Notes">Optional human-readable note for operators (not used by the projector).</param>
public sealed record AgentSkillsIoTarget(string Client, string RelativePath, string? Notes = null);

/// <summary>
/// Loaded representation of the agentskills.io target map.
/// </summary>
/// <param name="SchemaVersion">Schema version string (e.g. <c>dna.skills-projection.agentskills-io-targets.v1</c>).</param>
/// <param name="Targets">Per-client path entries.</param>
public sealed record AgentSkillsIoTargetMap(string SchemaVersion, IReadOnlyList<AgentSkillsIoTarget> Targets);

/// <summary>
/// Loads the agentskills.io target map from JSON. Tolerates the optional <c>$schema</c> and
/// <c>comment</c> fields so the config file is also valid against a JSON Schema validator.
/// </summary>
public static class AgentSkillsIoTargetMapLoader
{
    /// <summary>Load the map from a JSON file path.</summary>
    public static AgentSkillsIoTargetMap Load(string jsonPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath);
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"agentskills.io target map not found at '{jsonPath}'.", jsonPath);
        }
        return Parse(File.ReadAllText(jsonPath));
    }

    /// <summary>Parse the map from raw JSON text.</summary>
    public static AgentSkillsIoTargetMap Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
        var root = doc.RootElement;

        var schemaVersion = root.TryGetProperty("schemaVersion", out var sv)
            ? sv.GetString() ?? string.Empty
            : string.Empty;

        if (!root.TryGetProperty("targets", out var targetsElement) || targetsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("agentskills.io target map is missing the required 'targets' array.");
        }

        var targets = new List<AgentSkillsIoTarget>(targetsElement.GetArrayLength());
        foreach (var item in targetsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var client = item.GetProperty("client").GetString();
            var relativePath = item.GetProperty("relativePath").GetString();
            var notes = item.TryGetProperty("notes", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(client) || string.IsNullOrWhiteSpace(relativePath))
            {
                throw new InvalidDataException(
                    "agentskills.io target map entry is missing required 'client' or 'relativePath'.");
            }
            targets.Add(new AgentSkillsIoTarget(client.Trim(), relativePath.Trim(), notes));
        }

        return new AgentSkillsIoTargetMap(schemaVersion, targets);
    }
}
