// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Agents.LaneProfile;

public sealed class LaneProfileProjector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly LanePolicyRegistry _registry;
    private readonly TimeProvider _timeProvider;

    public LaneProfileProjector(LanePolicyRegistry registry, TimeProvider? timeProvider = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public LaneProjectionReceipt Project(LaneProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.TargetRoot))
            throw new ArgumentException("Target root is required.", nameof(request));

        var profile = _registry.FindProfile(request.ProfileId)
            ?? throw new LaneProfileProjectionException($"Unknown lane profile '{request.ProfileId}'.");

        if (profile.RequiresOperatorGate && !request.OperatorGateApproved)
        {
            throw new LaneProfileProjectionException($"Lane profile '{profile.ProfileId}' requires an explicit operator gate.");
        }

        var governancePacks = ResolveGovernance(profile).ToArray();
        var skillBundles = ResolveSkills(profile).ToArray();
        var validation = Validate(profile, governancePacks, skillBundles).ToArray();
        var artifacts = BuildArtifacts(request, profile, governancePacks, skillBundles, validation).ToArray();

        if (!request.DryRun)
        {
            Materialize(request.TargetRoot, artifacts);
        }

        return new LaneProjectionReceipt(
            profile.ProfileId,
            request.ActorId,
            request.StoryId,
            request.DryRun,
            _timeProvider.GetUtcNow(),
            artifacts,
            validation);
    }

    private IEnumerable<LaneGovernancePack> ResolveGovernance(LaneProfileDefinition profile)
    {
        foreach (var id in profile.RequiredGovernancePackIds.Concat(profile.OptionalSpecialistPackIds))
        {
            var pack = _registry.FindGovernancePack(id);
            if (pack is not null)
                yield return pack;
        }
    }

    private IEnumerable<LaneSkillBundle> ResolveSkills(LaneProfileDefinition profile)
    {
        foreach (var id in profile.SkillBundleIds)
        {
            var bundle = _registry.FindSkillBundle(id);
            if (bundle is not null)
                yield return bundle;
        }
    }

    private IEnumerable<LaneValidationResult> Validate(
        LaneProfileDefinition profile,
        IReadOnlyCollection<LaneGovernancePack> governancePacks,
        IReadOnlyCollection<LaneSkillBundle> skillBundles)
    {
        var missingGovernance = profile.RequiredGovernancePackIds
            .Where(id => governancePacks.All(pack => !string.Equals(pack.Id, id, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        yield return new LaneValidationResult(
            "non-waivable-governance",
            LaneValidationSeverity.Error,
            missingGovernance.Length == 0 && governancePacks.Any(pack => pack.NonWaivable),
            missingGovernance.Length == 0
                ? "Required non-waivable governance packs are present."
                : $"Missing governance packs: {string.Join(", ", missingGovernance)}.");

        var missingSkills = profile.SkillBundleIds
            .Where(id => skillBundles.All(bundle => !string.Equals(bundle.Id, id, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        yield return new LaneValidationResult(
            "skill-bundles",
            LaneValidationSeverity.Error,
            missingSkills.Length == 0,
            missingSkills.Length == 0
                ? "Skill bundles resolve to manifest references."
                : $"Missing skill bundles: {string.Join(", ", missingSkills)}.");

        yield return new LaneValidationResult(
            "mcp-dependencies",
            LaneValidationSeverity.Error,
            profile.McpDependencies.Where(dependency => dependency.Required).All(dependency => !string.IsNullOrWhiteSpace(dependency.ServiceId)),
            "Required MCP dependencies are declared by service id.");

        var envByReference = profile.EnvironmentReferences.All(reference =>
            !string.IsNullOrWhiteSpace(reference.Name) &&
            !string.IsNullOrWhiteSpace(reference.CredentialReference) &&
            !reference.CredentialReference.Contains('='));
        yield return new LaneValidationResult(
            "env-vars-by-reference",
            LaneValidationSeverity.Error,
            envByReference,
            envByReference
                ? "Environment variables render credential references only."
                : "Environment references must not contain inline values.");
    }

    private IEnumerable<LaneProjectionArtifact> BuildArtifacts(
        LaneProjectionRequest request,
        LaneProfileDefinition profile,
        IReadOnlyList<LaneGovernancePack> governancePacks,
        IReadOnlyList<LaneSkillBundle> skillBundles,
        IReadOnlyList<LaneValidationResult> validation)
    {
        yield return Artifact("profile.md", LaneProjectionArtifactKind.Markdown, RenderProfileMarkdown(request, profile, governancePacks, skillBundles));
        yield return Artifact("governance-packs.json", LaneProjectionArtifactKind.Json, governancePacks);
        yield return Artifact("skills.json", LaneProjectionArtifactKind.Json, skillBundles);
        yield return Artifact("mcp-services.json", LaneProjectionArtifactKind.Json, profile.McpDependencies);
        yield return Artifact("env.refs", LaneProjectionArtifactKind.EnvironmentReferences, RenderEnvRefs(profile.EnvironmentReferences));
        yield return Artifact("restart-handoff.md", LaneProjectionArtifactKind.Markdown, RenderRestartHandoff(request, profile));
        yield return Artifact("validation.json", LaneProjectionArtifactKind.Json, validation);
        yield return Artifact("receipt.json", LaneProjectionArtifactKind.Json, new
        {
            profile.ProfileId,
            request.ActorId,
            request.StoryId,
            request.DryRun,
            generatedAtUtc = _timeProvider.GetUtcNow(),
            artifacts = new[]
            {
                "profile.md",
                "governance-packs.json",
                "skills.json",
                "mcp-services.json",
                "env.refs",
                "restart-handoff.md",
                "validation.json"
            }
        });
    }

    private static LaneProjectionArtifact Artifact(string fileName, LaneProjectionArtifactKind kind, object value)
        => Artifact(fileName, kind, JsonSerializer.Serialize(value, JsonOptions));

    private static LaneProjectionArtifact Artifact(string fileName, LaneProjectionArtifactKind kind, string content)
        => new($".dna/lane-profile/{fileName}", kind, LaneProjectionAction.Create, content);

    private static string RenderProfileMarkdown(
        LaneProjectionRequest request,
        LaneProfileDefinition profile,
        IReadOnlyList<LaneGovernancePack> governancePacks,
        IReadOnlyList<LaneSkillBundle> skillBundles)
        => $"""
           # Lane Profile: {profile.DisplayName}

           Profile id: `{profile.ProfileId}`
           Actor: `{request.ActorId}`
           Story: `{request.StoryId ?? "unassigned"}`
           Capability tier: `{profile.CapabilityTier}`
           Dry run: `{request.DryRun}`

           ## Work Classes

           {Bullets(profile.AllowedWorkClasses.Select(item => item.ToString()))}

           ## Governance Packs

           {Bullets(governancePacks.Select(pack => $"{pack.Id} - {(pack.NonWaivable ? "non-waivable" : "specialist")}"))}

           ## Skill Bundles

           {Bullets(skillBundles.Select(bundle => $"{bundle.Id}: {string.Join(", ", bundle.SkillRefs)}"))}
           """;

    private static string RenderRestartHandoff(LaneProjectionRequest request, LaneProfileDefinition profile)
        => $"""
           # Lane Restart Handoff

           Before re-specializing away from `{profile.ProfileId}`, persist:

           - Session Persistence checkpoint for actor `{request.ActorId}`.
           - SDLC story note or checkpoint for `{request.StoryId ?? "the active assignment"}`.
           - Worktree path, branch, base ref, validation evidence, and next action.

           Required restart/reconfigure steps:

           {Bullets(profile.RestartRequirements.Select(requirement => $"{requirement.Id}: {requirement.Description}"))}
           """;

    private static string RenderEnvRefs(IReadOnlyList<LaneEnvironmentReference> references)
        => references.Count == 0
            ? "# No profile-specific environment references.\n"
            : string.Join(Environment.NewLine, references.Select(reference => $"{reference.Name} -> {reference.CredentialReference}")) + Environment.NewLine;

    private static string Bullets(IEnumerable<string> values)
        => string.Join(Environment.NewLine, values.Select(value => $"- {value}"));

    private static void Materialize(string targetRoot, IEnumerable<LaneProjectionArtifact> artifacts)
    {
        var root = Path.GetFullPath(targetRoot);
        foreach (var artifact in artifacts)
        {
            var path = Path.GetFullPath(Path.Combine(root, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            var safeRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                throw new LaneProfileProjectionException($"Artifact path escapes target root: {artifact.RelativePath}");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, artifact.Content);
        }
    }
}
