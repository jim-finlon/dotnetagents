namespace DotNetAgents.Agents.LaneProfile;

public enum LaneCapabilityTier
{
    Unknown,
    TierA,
    TierB,
    Privileged
}

public enum LaneWorkClass
{
    Unknown,
    UiFrontend,
    DotNetApi,
    PythonTooling,
    DocsOnly,
    PrivilegedLab,
    LocalOpenSourceLlmRunner
}

public enum LaneProjectionArtifactKind
{
    Markdown,
    Json,
    EnvironmentReferences
}

public enum LaneProjectionAction
{
    Create,
    Update,
    Unchanged
}

public enum LaneValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record LaneGovernancePack(
    string Id,
    string DisplayName,
    bool NonWaivable,
    IReadOnlyList<string> RuleRefs);

public sealed record LaneSkillBundle(
    string Id,
    string DisplayName,
    IReadOnlyList<string> SkillRefs);

public sealed record LaneMcpDependency(
    string ServiceId,
    bool Required,
    string? HealthRef = null);

public sealed record LaneEnvironmentReference(
    string Name,
    string CredentialReference,
    bool Required = true);

public sealed record LaneRestartRequirement(
    string Id,
    string Description,
    bool RequiresProcessRestart);

public sealed record LaneValidationCheck(
    string Id,
    string Description,
    LaneValidationSeverity Severity = LaneValidationSeverity.Error);

public sealed record LaneProfileDefinition(
    string ProfileId,
    string DisplayName,
    LaneCapabilityTier CapabilityTier,
    IReadOnlyList<LaneWorkClass> AllowedWorkClasses,
    IReadOnlyList<string> RequiredGovernancePackIds,
    IReadOnlyList<string> OptionalSpecialistPackIds,
    IReadOnlyList<string> SkillBundleIds,
    IReadOnlyList<LaneMcpDependency> McpDependencies,
    IReadOnlyList<LaneEnvironmentReference> EnvironmentReferences,
    IReadOnlyList<LaneRestartRequirement> RestartRequirements,
    IReadOnlyList<LaneValidationCheck> ValidationChecks,
    bool RequiresOperatorGate = false);

public sealed record LanePolicyRegistry(
    IReadOnlyList<LaneGovernancePack> GovernancePacks,
    IReadOnlyList<LaneSkillBundle> SkillBundles,
    IReadOnlyList<LaneProfileDefinition> Profiles)
{
    public LaneProfileDefinition? FindProfile(string profileId)
        => Profiles.FirstOrDefault(profile => string.Equals(profile.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));

    public LaneGovernancePack? FindGovernancePack(string id)
        => GovernancePacks.FirstOrDefault(pack => string.Equals(pack.Id, id, StringComparison.OrdinalIgnoreCase));

    public LaneSkillBundle? FindSkillBundle(string id)
        => SkillBundles.FirstOrDefault(bundle => string.Equals(bundle.Id, id, StringComparison.OrdinalIgnoreCase));
}

public sealed record LaneProjectionRequest(
    string ProfileId,
    string TargetRoot,
    string ActorId,
    string? StoryId = null,
    bool DryRun = true,
    bool OperatorGateApproved = false);

public sealed record LaneProjectionArtifact(
    string RelativePath,
    LaneProjectionArtifactKind Kind,
    LaneProjectionAction Action,
    string Content);

public sealed record LaneValidationResult(
    string CheckId,
    LaneValidationSeverity Severity,
    bool Passed,
    string Message);

public sealed record LaneProjectionReceipt(
    string ProfileId,
    string ActorId,
    string? StoryId,
    bool DryRun,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<LaneProjectionArtifact> Artifacts,
    IReadOnlyList<LaneValidationResult> ValidationResults)
{
    public bool IsValid => ValidationResults.All(result => result.Passed || result.Severity != LaneValidationSeverity.Error);
}

public sealed class LaneProfileProjectionException(string message) : InvalidOperationException(message);
