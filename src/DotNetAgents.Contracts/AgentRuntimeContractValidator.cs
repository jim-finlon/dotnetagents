// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Contracts;

public static class AgentRuntimeContractValidator
{
    private static readonly string[] RequiredAgentTelemetryAttributes =
    [
        "agent.id",
        "chain.id",
        "model.id",
        "llm.prompt.ref",
        "task.domain"
    ];

    private static readonly string[] RequiredChainTelemetryAttributes =
    [
        "agent.id",
        "chain.id",
        "chain.step.id",
        "llm.prompt.ref",
        "tool.name"
    ];

    public static AgentRuntimeContractValidationResult Validate(AgentContract? contract)
    {
        var findings = new List<AgentRuntimeContractValidationFinding>();
        if (contract is null)
        {
            findings.Add(Error("missing_agent_contract", "Agent contract is required."));
            return new AgentRuntimeContractValidationResult(false, findings);
        }

        Required(contract.SchemaVersion, "schema_version_missing", "SchemaVersion is required.", findings);
        if (!string.Equals(contract.SchemaVersion, AgentContract.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            findings.Add(Error("schema_version_mismatch", $"Expected {AgentContract.CurrentSchemaVersion}; got '{contract.SchemaVersion}'."));
        }

        Required(contract.AgentId, "agent_id_missing", "AgentId is required.", findings);
        Required(contract.DisplayName, "display_name_missing", "DisplayName is required.", findings);
        Required(contract.CognitionProfileRef, "cognition_profile_ref_missing", "CognitionProfileRef is required.", findings);

        if (contract.Capabilities.Count == 0)
            findings.Add(Error("capabilities_missing", "At least one capability is required."));
        if (contract.ChainContractRefs.Count == 0)
            findings.Add(Error("chain_contract_refs_missing", "At least one chain contract reference is required."));
        if (contract.ForbiddenSurfaces.Count == 0)
            findings.Add(Error("forbidden_surfaces_missing", "At least one forbidden surface is required."));
        if (contract.EvaluationSuites.Count == 0)
            findings.Add(Error("evaluation_suites_missing", "At least one evaluation suite is required."));

        ValidateInstructionArtifacts(contract.InstructionArtifacts, findings);
        ValidateModelPolicy(contract.ModelPolicy, findings);
        ValidateToolPolicy(contract.ToolPolicy, findings);
        ValidateMemoryPolicy(contract.MemoryPolicy, findings);
        ValidateTelemetry(contract.Telemetry, RequiredAgentTelemetryAttributes, "agent", findings);

        return new AgentRuntimeContractValidationResult(findings.All(f => f.Severity != "error"), findings);
    }

    public static AgentRuntimeContractValidationResult Validate(ChainContract? contract)
    {
        var findings = new List<AgentRuntimeContractValidationFinding>();
        if (contract is null)
        {
            findings.Add(Error("missing_chain_contract", "Chain contract is required."));
            return new AgentRuntimeContractValidationResult(false, findings);
        }

        Required(contract.SchemaVersion, "schema_version_missing", "SchemaVersion is required.", findings);
        if (!string.Equals(contract.SchemaVersion, ChainContract.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            findings.Add(Error("schema_version_mismatch", $"Expected {ChainContract.CurrentSchemaVersion}; got '{contract.SchemaVersion}'."));
        }

        Required(contract.ChainId, "chain_id_missing", "ChainId is required.", findings);
        Required(contract.DisplayName, "display_name_missing", "DisplayName is required.", findings);
        Required(contract.Intent, "intent_missing", "Intent is required.", findings);
        Required(contract.EntryPointStepId, "entrypoint_step_missing", "EntryPointStepId is required.", findings);

        if (contract.Steps.Count == 0)
        {
            findings.Add(Error("steps_missing", "At least one chain step is required."));
        }
        else
        {
            ValidateSteps(contract, findings);
        }

        if (contract.ForbiddenSurfaces.Count == 0)
            findings.Add(Error("forbidden_surfaces_missing", "At least one forbidden surface is required."));

        ValidateMemoryPolicy(contract.MemoryPolicy, findings);
        ValidateTelemetry(contract.Telemetry, RequiredChainTelemetryAttributes, "chain", findings);

        return new AgentRuntimeContractValidationResult(findings.All(f => f.Severity != "error"), findings);
    }

    private static void ValidateSteps(ChainContract contract, List<AgentRuntimeContractValidationFinding> findings)
    {
        var stepsById = new Dictionary<string, ChainContractStep>(StringComparer.Ordinal);
        foreach (var step in contract.Steps)
        {
            Required(step.StepId, "step_id_missing", "Each chain step requires StepId.", findings);
            Required(step.DisplayName, "step_display_name_missing", "Each chain step requires DisplayName.", findings);
            Required(step.Kind, "step_kind_missing", "Each chain step requires Kind.", findings);

            if (!string.IsNullOrWhiteSpace(step.StepId) && !stepsById.TryAdd(step.StepId, step))
                findings.Add(Error("duplicate_step_id", $"StepId '{step.StepId}' is declared more than once."));
        }

        if (!stepsById.ContainsKey(contract.EntryPointStepId))
            findings.Add(Error("entrypoint_step_not_found", $"EntryPointStepId '{contract.EntryPointStepId}' does not match any step."));

        foreach (var step in contract.Steps)
        {
            foreach (var nextStepId in step.NextStepIds)
            {
                if (!stepsById.ContainsKey(nextStepId))
                    findings.Add(Error("next_step_not_found", $"Step '{step.StepId}' points to missing next step '{nextStepId}'."));
            }
        }
    }

    private static void ValidateInstructionArtifacts(AgentInstructionArtifacts? instructionArtifacts, List<AgentRuntimeContractValidationFinding> findings)
    {
        if (instructionArtifacts is null)
        {
            findings.Add(Error("instruction_artifacts_missing", "InstructionArtifacts are required."));
            return;
        }

        Required(instructionArtifacts.SystemPromptRef, "system_prompt_ref_missing", "SystemPromptRef is required.", findings);
    }

    private static void ValidateModelPolicy(AgentModelPolicy? modelPolicy, List<AgentRuntimeContractValidationFinding> findings)
    {
        if (modelPolicy is null)
        {
            findings.Add(Error("model_policy_missing", "ModelPolicy is required."));
            return;
        }

        Required(modelPolicy.RouterRef, "router_ref_missing", "RouterRef is required.", findings);
        Required(modelPolicy.DefaultModelRef, "default_model_ref_missing", "DefaultModelRef is required.", findings);
        Required(modelPolicy.CostPolicy, "cost_policy_missing", "CostPolicy is required.", findings);
    }

    private static void ValidateToolPolicy(AgentToolPolicy? toolPolicy, List<AgentRuntimeContractValidationFinding> findings)
    {
        if (toolPolicy is null)
        {
            findings.Add(Error("tool_policy_missing", "ToolPolicy is required."));
            return;
        }

        var duplicateAllowed = toolPolicy.DeterministicToolRefs
            .Intersect(toolPolicy.ForbiddenToolRefs, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var toolRef in duplicateAllowed)
            findings.Add(Error("tool_policy_conflict", $"Tool '{toolRef}' is both deterministic/allowed and forbidden."));
    }

    private static void ValidateMemoryPolicy(AgentMemoryPolicy? memoryPolicy, List<AgentRuntimeContractValidationFinding> findings)
    {
        if (memoryPolicy is null)
        {
            findings.Add(Error("memory_policy_missing", "MemoryPolicy is required."));
            return;
        }

        Required(memoryPolicy.ProjectionPolicy, "memory_projection_policy_missing", "ProjectionPolicy is required.", findings);
    }

    private static void ValidateTelemetry(
        AgentContractTelemetryPolicy? telemetry,
        IReadOnlyList<string> requiredAttributes,
        string contractKind,
        List<AgentRuntimeContractValidationFinding> findings)
    {
        if (telemetry is null)
        {
            findings.Add(Error("telemetry_policy_missing", "Telemetry policy is required."));
            return;
        }

        foreach (var attribute in requiredAttributes)
        {
            if (!telemetry.RequiredSpanAttributes.Contains(attribute, StringComparer.Ordinal))
                findings.Add(Error("telemetry_attribute_missing", $"Required {contractKind} span attribute '{attribute}' is missing."));
        }

        if (telemetry.RequiredMetricRefs.Count == 0)
            findings.Add(Error("telemetry_metric_refs_missing", $"At least one {contractKind} metric reference is required."));
        if (telemetry.RequiredOutcomeRefs.Count == 0)
            findings.Add(Error("telemetry_outcome_refs_missing", $"At least one {contractKind} outcome reference is required."));
    }

    private static void Required(string? value, string code, string message, List<AgentRuntimeContractValidationFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(value))
            findings.Add(Error(code, message));
    }

    private static AgentRuntimeContractValidationFinding Error(string code, string message)
        => new("error", code, message);
}

public sealed record AgentRuntimeContractValidationResult(
    bool IsValid,
    IReadOnlyList<AgentRuntimeContractValidationFinding> Findings);

public sealed record AgentRuntimeContractValidationFinding(
    string Severity,
    string Code,
    string Message);
