// SPDX-License-Identifier: Apache-2.0

// Story c4b3b3e5 — moved from DotNetAgents.Agents.Supervisor to
// DotNetAgents.Workflow with IQualityGate.
namespace DotNetAgents.Workflow;

/// <summary>
/// Result of a quality gate evaluation.
/// Domain-specific decisions: EducationAgent uses Advance/Reinforce/Remediate;
/// PublishingAgent uses Pass/Conditional/Fail.
/// </summary>
public record QualityGateResult
{
    /// <summary>
    /// Well-known decision constants (e.g. Pass, Conditional, Fail).
    /// </summary>
    public static class Decisions
    {
        public const string Pass = "Pass";
        public const string Conditional = "Conditional";
        public const string Fail = "Fail";
    }

    /// <summary>
    /// The routing decision (e.g., "Advance", "Remediate", "Pass", "Fail", "Conditional").
    /// </summary>
    public string Decision { get; init; } = string.Empty;

    /// <summary>
    /// Mandatory revision targets when decision is Conditional.
    /// </summary>
    public IReadOnlyList<string>? MandatoryTargets { get; init; }

    /// <summary>
    /// Optional reasoning for the decision.
    /// </summary>
    public string? Reasoning { get; init; }
}
