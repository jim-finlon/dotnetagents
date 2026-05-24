// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.LaneOps;

/// <summary>
/// Versioned bundle declaring the rules a <see cref="RunnerRoutingPolicyEngine"/> applies.
/// Story 58b726c9: extracts <see cref="RunnerRoutingPolicy"/> hard-coded rules into a
/// JSON config bundle (default at <c>config/runner-routing/v1.json</c>) so operators can
/// amend rules without recompiling and so multiple policy versions can coexist for
/// audit replay.
/// </summary>
public sealed record RunnerRoutingBundle(
    string PolicyVersion,
    IReadOnlyList<RunnerRoutingRule> Rules,
    string UnknownWorkloadRefusalReasonTemplate)
{
    /// <summary>JSON deserialization options used by <see cref="LoadFromFile"/> and <see cref="LoadFromJson"/>.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Load a bundle from a JSON file on disk.</summary>
    /// <param name="path">Absolute or working-directory-relative file path.</param>
    public static RunnerRoutingBundle LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    /// <summary>Load a bundle from an in-memory JSON string. Useful for tests and embedded defaults.</summary>
    public static RunnerRoutingBundle LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var bundle = JsonSerializer.Deserialize<RunnerRoutingBundle>(json, JsonOptions)
            ?? throw new InvalidOperationException("RunnerRoutingBundle JSON deserialized to null.");
        Validate(bundle);
        return bundle;
    }

    private static void Validate(RunnerRoutingBundle bundle)
    {
        if (string.IsNullOrWhiteSpace(bundle.PolicyVersion))
            throw new InvalidOperationException("RunnerRoutingBundle.policyVersion is required.");
        if (string.IsNullOrWhiteSpace(bundle.UnknownWorkloadRefusalReasonTemplate))
            throw new InvalidOperationException("RunnerRoutingBundle.unknownWorkloadRefusalReasonTemplate is required.");
        if (bundle.Rules.Count == 0)
            throw new InvalidOperationException("RunnerRoutingBundle.rules must contain at least one rule.");

        foreach (var rule in bundle.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Name))
                throw new InvalidOperationException("RunnerRoutingRule.name is required.");
            if (rule.WorkloadClasses.Count == 0)
                throw new InvalidOperationException($"RunnerRoutingRule '{rule.Name}' must declare at least one workload class.");
            if (rule.PrimaryRunnerClass == RunnerClass.Unspecified)
                throw new InvalidOperationException($"RunnerRoutingRule '{rule.Name}' must declare a non-Unspecified primaryRunnerClass.");
        }
    }
}

/// <summary>Single rule keyed by one or more workload classes. Order in <see cref="RunnerRoutingBundle.Rules"/> is significant.</summary>
public sealed record RunnerRoutingRule(
    string Name,
    IReadOnlyList<string> WorkloadClasses,
    RunnerClass PrimaryRunnerClass,
    string PrimaryReasonTemplate,
    IReadOnlyList<RunnerClass>? ForbiddenPreferredRunnerClasses = null,
    string? PreferredRunnerClassRefusalReasonTemplate = null,
    IReadOnlyList<RunnerRoutingAlternate>? Alternates = null,
    IReadOnlyList<RunnerClass>? HighBlastRefusedPreferredRunnerClasses = null,
    string? HighBlastPreferredRefusalReasonTemplate = null);

/// <summary>
/// Conditional alternate runner class for a rule. Applies before the primary fallback when
/// every requirement is satisfied. Story 58b726c9.
/// </summary>
public sealed record RunnerRoutingAlternate(
    RunnerClass RunnerClass,
    string ReasonTemplate,
    string? RequiresAutonomyTier = null,
    bool RequiresOperatorAllowsLocal = false,
    bool RefusedIfHighBlastRadius = false,
    bool RequiresK3sPodEligible = false,
    bool RefusedIfRequiresPrivilegedSyscalls = false,
    bool RefusedIfRequiresKernelModules = false);
