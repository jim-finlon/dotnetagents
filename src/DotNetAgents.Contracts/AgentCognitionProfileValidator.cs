// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Contracts;

/// <summary>Validation helpers for the DNA Agent cognition contract.</summary>
public static class AgentCognitionProfileValidator
{
    private static readonly string[] RequiredTelemetryAttributes =
    [
        "agent.id",
        "model.id",
        "model.routing.policy",
        "llm.prompt.ref",
        "task.domain"
    ];

    public static AgentCognitionValidationResult Validate(AgentCognitionProfile? profile)
    {
        var findings = new List<AgentCognitionValidationFinding>();
        if (profile is null)
        {
            findings.Add(Error("missing_profile", "A true Agent must declare a cognition profile."));
            return new AgentCognitionValidationResult(false, findings);
        }

        Required(profile.SchemaVersion, "schema_version_missing", "SchemaVersion is required.", findings);
        if (!string.Equals(profile.SchemaVersion, AgentCognitionProfile.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            findings.Add(Error("schema_version_mismatch", $"Expected {AgentCognitionProfile.CurrentSchemaVersion}; got '{profile.SchemaVersion}'."));
        }

        Required(profile.ProfileId, "profile_id_missing", "ProfileId is required.", findings);
        Required(profile.ReasoningRole, "reasoning_role_missing", "ReasoningRole is required.", findings);

        if (!profile.LlmBacked)
        {
            findings.Add(Error("agent_not_llm_backed", "True Agents must be LLM-backed. Use Tool, Worker, Service, or McpServer for deterministic automation."));
        }

        ValidateReasoning(profile.ReasoningProfile, findings);
        ValidateModelPolicy(profile.ModelPolicy, findings);
        ValidatePromptChain(profile.PromptChain, findings);
        ValidateToolPolicy(profile.ToolPolicy, findings);
        ValidateMemoryPolicy(profile.MemoryPolicy, findings);
        ValidateTelemetry(profile.Telemetry, findings);
        ValidateEvaluationSandbox(profile.EvaluationSandbox, findings);

        return new AgentCognitionValidationResult(findings.All(f => f.Severity != "error"), findings);
    }

    private static void ValidateReasoning(AgentReasoningProfile? reasoning, List<AgentCognitionValidationFinding> findings)
    {
        if (reasoning is null)
        {
            findings.Add(Error("reasoning_profile_missing", "ReasoningProfile is required."));
            return;
        }

        Required(reasoning.DefaultCognitionTier, "default_cognition_tier_missing", "DefaultCognitionTier is required.", findings);
        Required(reasoning.MinimumReasoningEffort, "minimum_reasoning_effort_missing", "MinimumReasoningEffort is required.", findings);
        Required(reasoning.EscalationPolicy, "escalation_policy_missing", "EscalationPolicy is required.", findings);
        if (reasoning.AllowedCognitionTiers.Count == 0)
            findings.Add(Error("allowed_cognition_tiers_missing", "At least one allowed cognition tier is required."));
    }

    private static void ValidateModelPolicy(AgentModelPolicy? modelPolicy, List<AgentCognitionValidationFinding> findings)
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

    private static void ValidatePromptChain(AgentPromptChainContract? promptChain, List<AgentCognitionValidationFinding> findings)
    {
        if (promptChain is null)
        {
            findings.Add(Error("prompt_chain_missing", "PromptChain is required."));
            return;
        }

        Required(promptChain.SystemPromptRef, "system_prompt_ref_missing", "SystemPromptRef is required.", findings);
        Required(promptChain.ChainContractRef, "chain_contract_ref_missing", "ChainContractRef is required.", findings);
    }

    private static void ValidateToolPolicy(AgentToolPolicy? toolPolicy, List<AgentCognitionValidationFinding> findings)
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

    private static void ValidateMemoryPolicy(AgentMemoryPolicy? memoryPolicy, List<AgentCognitionValidationFinding> findings)
    {
        if (memoryPolicy is null)
        {
            findings.Add(Error("memory_policy_missing", "MemoryPolicy is required."));
            return;
        }

        Required(memoryPolicy.ProjectionPolicy, "memory_projection_policy_missing", "ProjectionPolicy is required.", findings);
    }

    private static void ValidateTelemetry(AgentTelemetryPolicy? telemetry, List<AgentCognitionValidationFinding> findings)
    {
        if (telemetry is null)
        {
            findings.Add(Error("telemetry_policy_missing", "Telemetry policy is required."));
            return;
        }

        foreach (var attribute in RequiredTelemetryAttributes)
        {
            if (!telemetry.RequiredSpanAttributes.Contains(attribute, StringComparer.Ordinal))
                findings.Add(Error("telemetry_attribute_missing", $"Required span attribute '{attribute}' is missing."));
        }
    }

    private static void ValidateEvaluationSandbox(AgentEvaluationSandboxPolicy? evaluationSandbox, List<AgentCognitionValidationFinding> findings)
    {
        if (evaluationSandbox is null)
        {
            findings.Add(Error("evaluation_sandbox_policy_missing", "EvaluationSandbox policy is required."));
            return;
        }

        if (evaluationSandbox.EvaluationSuites.Count == 0)
            findings.Add(Error("evaluation_sandbox_evaluation_suites_missing", "At least one EvaluationSandbox evaluation suite is required."));
        if (evaluationSandbox.VariantableSurfaces.Count == 0)
            findings.Add(Error("evaluation_sandbox_variantable_surfaces_missing", "At least one variantable cognition surface is required."));
        if (evaluationSandbox.FitnessSignals.Count == 0)
            findings.Add(Error("evaluation_sandbox_fitness_signals_missing", "At least one fitness signal is required."));
    }

    private static void Required(string? value, string code, string message, List<AgentCognitionValidationFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(value))
            findings.Add(Error(code, message));
    }

    private static AgentCognitionValidationFinding Error(string code, string message)
        => new("error", code, message);
}

public sealed record AgentCognitionValidationResult(
    bool IsValid,
    IReadOnlyList<AgentCognitionValidationFinding> Findings);

public sealed record AgentCognitionValidationFinding(
    string Severity,
    string Code,
    string Message);
