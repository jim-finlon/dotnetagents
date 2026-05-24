// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Workflows;

/// <summary>
/// Builder-facing memory scopes for a typed workflow step. These scopes describe
/// context breadth; runners map them onto concrete memory surfaces at execution.
/// </summary>
public enum WorkflowStepMemoryScopeKind
{
    /// <summary>The step receives no prior workflow memory.</summary>
    None = 0,
    /// <summary>The step may read only the immediately preceding step output.</summary>
    PreviousStep = 1,
    /// <summary>The step may read explicitly selected upstream step outputs.</summary>
    SelectedSteps = 2,
    /// <summary>The step may read the current session memory.</summary>
    Session = 3,
    /// <summary>The step may read project/workspace memory.</summary>
    Project = 4,
    /// <summary>The step may read global curated lessons.</summary>
    GlobalLessons = 5
}

/// <summary>Action-step permission and evidence declaration.</summary>
/// <param name="ToolAllowlist">Exact tool, connector, or action ids the step may invoke.</param>
/// <param name="RequiresApproval">Whether a human or policy gate must approve execution first.</param>
/// <param name="EvidenceOutputs">Evidence artifacts the action must emit for audit/completion.</param>
public sealed record WorkflowStepActionPolicy(
    IReadOnlyList<string> ToolAllowlist,
    bool RequiresApproval,
    IReadOnlyList<string> EvidenceOutputs);

/// <summary>
/// Inspectable workflow-builder step contract used before compiling to a runtime
/// <see cref="IWorkflowStep"/> implementation.
/// </summary>
public sealed record WorkflowStepContract(
    string Id,
    WorkflowStepKind Kind,
    IReadOnlyList<WorkflowStepMemoryScopeKind> MemoryScopes,
    IReadOnlyList<string>? SelectedStepIds = null,
    WorkflowStepActionPolicy? ActionPolicy = null,
    string? FallbackBehavior = null);

/// <summary>A validation issue found in a workflow-builder step contract.</summary>
public sealed record WorkflowStepContractValidationIssue(string Code, string Message, string? StepId = null);

/// <summary>Validation helpers for workflow-builder v1 typed step contracts.</summary>
public static class WorkflowStepContractValidator
{
    private static readonly WorkflowStepKind[] V1BuilderKinds =
    [
        WorkflowStepKind.Retrieve,
        WorkflowStepKind.Reason,
        WorkflowStepKind.ReadSource,
        WorkflowStepKind.Act,
        WorkflowStepKind.Respond,
        WorkflowStepKind.PolicyGate,
        WorkflowStepKind.HumanConfirm
    ];

    private static readonly HashSet<string> ArbitraryExecutionToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "*",
        "bash",
        "cmd",
        "powershell",
        "script",
        "shell",
        "terminal"
    };

    /// <summary>Returns the seven non-trigger v1 builder step kinds in presentation order.</summary>
    public static IReadOnlyList<WorkflowStepKind> GetV1BuilderKinds() => V1BuilderKinds;

    /// <summary>Validate one workflow-builder step declaration.</summary>
    public static IReadOnlyList<WorkflowStepContractValidationIssue> Validate(WorkflowStepContract step)
    {
        ArgumentNullException.ThrowIfNull(step);

        var issues = new List<WorkflowStepContractValidationIssue>();
        if (string.IsNullOrWhiteSpace(step.Id))
        {
            issues.Add(new("step.idRequired", "Step id is required.", step.Id));
        }

        ValidateMemoryScopes(step, issues);

        if (step.Kind == WorkflowStepKind.Act)
        {
            ValidateActionPolicy(step, issues);
        }

        if (step.ActionPolicy is not null && step.Kind != WorkflowStepKind.Act)
        {
            issues.Add(new(
                "step.actionPolicyOnlyOnAct",
                "Only act steps may declare an action policy.",
                step.Id));
        }

        if (string.IsNullOrWhiteSpace(step.FallbackBehavior))
        {
            issues.Add(new(
                "step.fallbackRequired",
                "Every workflow-builder step must declare fallback behavior.",
                step.Id));
        }

        return issues;
    }

    private static void ValidateMemoryScopes(WorkflowStepContract step, ICollection<WorkflowStepContractValidationIssue> issues)
    {
        if (step.MemoryScopes.Count == 0)
        {
            issues.Add(new("step.memoryScopeRequired", "At least one memory scope must be declared.", step.Id));
            return;
        }

        if (step.MemoryScopes.Contains(WorkflowStepMemoryScopeKind.None) && step.MemoryScopes.Count > 1)
        {
            issues.Add(new(
                "step.noneMemoryScopeExclusive",
                "The none memory scope cannot be combined with any other memory scope.",
                step.Id));
        }

        if (step.MemoryScopes.Contains(WorkflowStepMemoryScopeKind.SelectedSteps)
            && (step.SelectedStepIds is null || step.SelectedStepIds.Count == 0))
        {
            issues.Add(new(
                "step.selectedStepsRequired",
                "Selected-steps memory scope must name the upstream step ids it may read.",
                step.Id));
        }

        if (step.MemoryScopes.Contains(WorkflowStepMemoryScopeKind.GlobalLessons)
            && step.Kind is WorkflowStepKind.Act or WorkflowStepKind.PolicyGate or WorkflowStepKind.HumanConfirm)
        {
            issues.Add(new(
                "step.globalLessonsNotAllowed",
                "Global lessons may not be read by action, policy-gate, or human-confirm steps.",
                step.Id));
        }
    }

    private static void ValidateActionPolicy(WorkflowStepContract step, ICollection<WorkflowStepContractValidationIssue> issues)
    {
        if (step.ActionPolicy is null)
        {
            issues.Add(new(
                "act.actionPolicyRequired",
                "Act steps must declare tool allowlists, approval requirements, and evidence outputs.",
                step.Id));
            return;
        }

        if (step.ActionPolicy.ToolAllowlist.Count == 0)
        {
            issues.Add(new("act.toolAllowlistRequired", "Act steps must declare at least one allowed tool.", step.Id));
        }

        if (step.ActionPolicy.ToolAllowlist.Any(static tool => ArbitraryExecutionToolIds.Contains(tool.Trim())))
        {
            issues.Add(new(
                "act.arbitraryExecutionNotAllowed",
                "Act steps may not allow arbitrary script or shell execution in the v1 taxonomy.",
                step.Id));
        }

        if (step.ActionPolicy.EvidenceOutputs.Count == 0)
        {
            issues.Add(new("act.evidenceOutputsRequired", "Act steps must declare evidence outputs.", step.Id));
        }
    }
}
