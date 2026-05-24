// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Workflows;

/// <summary>
/// Builder-facing workflow definition before service-specific compilation.
/// Trigger/input is represented outside the step chain by <paramref name="TriggerInputSchema"/>.
/// </summary>
public sealed record WorkflowDefinitionContract(
    string Id,
    string TriggerInputSchema,
    IReadOnlyList<WorkflowStepContract> Steps);

/// <summary>Inspectable row for UI/API previews of workflow-builder declarations.</summary>
public sealed record WorkflowStepPreview(
    string Id,
    WorkflowStepKind Kind,
    IReadOnlyList<WorkflowStepMemoryScopeKind> MemoryScopes,
    bool IsRiskyAction,
    IReadOnlyList<string> ToolAllowlist,
    string? FallbackBehavior);

/// <summary>Workflow-level validation and preview helpers for v1 builder definitions.</summary>
public static class WorkflowDefinitionContractValidator
{
    public static IReadOnlyList<WorkflowStepContractValidationIssue> Validate(WorkflowDefinitionContract workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        var issues = new List<WorkflowStepContractValidationIssue>();

        if (string.IsNullOrWhiteSpace(workflow.Id))
        {
            issues.Add(new("workflow.idRequired", "Workflow id is required."));
        }

        if (string.IsNullOrWhiteSpace(workflow.TriggerInputSchema))
        {
            issues.Add(new("workflow.triggerInputSchemaRequired", "Trigger/input schema is required."));
        }

        if (workflow.Steps.Count == 0)
        {
            issues.Add(new("workflow.stepsRequired", "At least one workflow step is required."));
            return issues;
        }

        foreach (var step in workflow.Steps)
        {
            issues.AddRange(WorkflowStepContractValidator.Validate(step));
        }

        ValidateUniqueStepIds(workflow, issues);
        ValidateSelectedStepReferences(workflow, issues);
        ValidateReadSourceFollowsRetrieve(workflow, issues);

        return issues;
    }

    public static IReadOnlyList<WorkflowStepPreview> Preview(WorkflowDefinitionContract workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        return workflow.Steps
            .Select(step => new WorkflowStepPreview(
                step.Id,
                step.Kind,
                step.MemoryScopes,
                IsRiskyAction: step.Kind == WorkflowStepKind.Act,
                ToolAllowlist: step.ActionPolicy?.ToolAllowlist ?? [],
                step.FallbackBehavior))
            .ToList();
    }

    private static void ValidateUniqueStepIds(
        WorkflowDefinitionContract workflow,
        ICollection<WorkflowStepContractValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in workflow.Steps)
        {
            if (!string.IsNullOrWhiteSpace(step.Id) && !seen.Add(step.Id))
            {
                issues.Add(new("workflow.duplicateStepId", "Workflow step ids must be unique.", step.Id));
            }
        }
    }

    private static void ValidateSelectedStepReferences(
        WorkflowDefinitionContract workflow,
        ICollection<WorkflowStepContractValidationIssue> issues)
    {
        var previousStepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in workflow.Steps)
        {
            if (step.SelectedStepIds is not null)
            {
                foreach (var selectedStepId in step.SelectedStepIds)
                {
                    if (!previousStepIds.Contains(selectedStepId))
                    {
                        issues.Add(new(
                            "workflow.selectedStepMustBeUpstream",
                            "Selected memory scope may only reference upstream step ids.",
                            step.Id));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(step.Id))
            {
                previousStepIds.Add(step.Id);
            }
        }
    }

    private static void ValidateReadSourceFollowsRetrieve(
        WorkflowDefinitionContract workflow,
        ICollection<WorkflowStepContractValidationIssue> issues)
    {
        var hasRetrieveBefore = false;

        foreach (var step in workflow.Steps)
        {
            if (step.Kind == WorkflowStepKind.Retrieve)
            {
                hasRetrieveBefore = true;
            }

            if (step.Kind == WorkflowStepKind.ReadSource && !hasRetrieveBefore)
            {
                issues.Add(new(
                    "workflow.readSourceRequiresPriorRetrieve",
                    "Read-source steps require an upstream retrieve/search step in v1.",
                    step.Id));
            }
        }
    }
}
