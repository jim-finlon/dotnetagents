namespace DotNetAgents.Runtime;

public enum AgentProgramRiskClass
{
    Low,
    Moderate,
    High
}

public enum AgentProgramModuleKind
{
    PromptModule,
    ToolCallingModule,
    RetrievalAugmentedModule,
    DelegatedAgentModule,
    BehaviorTreeModule,
    ScheduledModule,
    AssemblyRuleModule
}

public enum AgentProgramComponentKind
{
    PromptGene,
    Skill,
    Toolset,
    RetrievalPolicy,
    MemoryPolicy,
    BehaviorTree,
    AgentProgram,
    EvaluatorRubric,
    RuntimeAssemblyRule,
    ModelRoutePolicy
}

public enum AgentProgramPrivacyClass
{
    Public,
    Internal,
    Restricted,
    SecretReferenceOnly
}

public enum AgentProgramEvidenceClass
{
    ExternalClaim,
    LocalValidationHarness,
    EmpiricalBenchmark,
    OperatorObservation
}

public enum AgentProgramControlFlowKind
{
    Sequence,
    Branch,
    Retry,
    Delegate,
    Schedule,
    BehaviorTree
}

public sealed record AgentProgramField(
    string Name,
    string TypeRef,
    string SourceRef,
    AgentProgramPrivacyClass PrivacyClass,
    IReadOnlyList<string> ValidationRefs);

public sealed record AgentProgramSignature(
    IReadOnlyList<AgentProgramField> Inputs,
    IReadOnlyList<AgentProgramField> Outputs);

public sealed record AgentProgramComponentRef(
    AgentProgramComponentKind Kind,
    string Ref,
    string? Version,
    string Purpose);

public sealed record AgentProgramModule(
    string ModuleId,
    AgentProgramModuleKind Kind,
    AgentProgramSignature Signature,
    string? ModelRoutePolicyRef,
    IReadOnlyList<AgentProgramComponentRef> PromptComponents,
    string? RetrievalPolicyRef,
    string? ToolsetRef,
    IReadOnlyList<string> SkillRefs,
    string? MemoryPolicyRef,
    IReadOnlyList<AgentProgramComponentRef> ComponentRefs);

public sealed record AgentProgramControlFlow(
    AgentProgramControlFlowKind Kind,
    IReadOnlyList<string> ModuleOrder,
    IReadOnlyList<AgentProgramComponentRef> ControlComponentRefs);

public sealed record AgentProgramEvaluatorRef(
    string MetricId,
    string DatasetSplitId,
    string ScorerRef,
    double PromotionThreshold,
    string CostBudgetRef);

public sealed record AgentProgramOptimizerPolicyRef(
    string PolicyId,
    IReadOnlyList<string> AllowedStrategyIds,
    decimal MaxCostUsd,
    int MaxRollouts,
    string DatasetSplitId,
    string PromotionGateRef,
    string RollbackPolicyRef,
    bool HumanReviewRequired);

public sealed record AgentProgramProvenance(
    IReadOnlyList<string> SourceStoryIds,
    IReadOnlyList<string> SourceDocumentRefs,
    IReadOnlyList<string> ExternalInspirationRefs,
    string LicensePosture,
    AgentProgramEvidenceClass EvidenceClass);

public sealed record AgentProgramDefinition
{
    public string ProgramId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string OwnerActorId { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public AgentProgramRiskClass RiskClass { get; init; } = AgentProgramRiskClass.Moderate;
    public IReadOnlyList<AgentProgramField> Inputs { get; init; } = [];
    public IReadOnlyList<AgentProgramField> Outputs { get; init; } = [];
    public IReadOnlyList<AgentProgramModule> Modules { get; init; } = [];
    public AgentProgramControlFlow? ControlFlow { get; init; }
    public IReadOnlyList<AgentProgramEvaluatorRef> Evaluators { get; init; } = [];
    public AgentProgramOptimizerPolicyRef? OptimizerPolicy { get; init; }
    public AgentProgramProvenance? Provenance { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record AgentCompiledVariant
{
    public string VariantId { get; init; } = string.Empty;
    public string SourceProgramId { get; init; } = string.Empty;
    public string SourceProgramVersion { get; init; } = string.Empty;
    public string CompileStrategyId { get; init; } = string.Empty;
    public IReadOnlyList<AgentProgramComponentRef> FrozenComponentRefs { get; init; } = [];
    public IReadOnlyList<AgentProgramEvaluatorRef> EvaluatorRefs { get; init; } = [];
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];
    public IReadOnlyList<string> BudgetSummaryRefs { get; init; } = [];
    public RollbackRef? RollbackRef { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record AgentProgramValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed class AgentProgramDefinitionValidator
{
    private static readonly string[] SecretPayloadMarkers =
    [
        "api_key=",
        "apikey=",
        "password=",
        "secret=",
        "token=",
        "bearer ",
        "sk-"
    ];

    public AgentProgramValidationResult Validate(AgentProgramDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var errors = new List<string>();

        Require(definition.ProgramId, "program_id_required", errors);
        Require(definition.Version, "version_required", errors);
        Require(definition.OwnerActorId, "owner_actor_id_required", errors);
        Require(definition.Purpose, "purpose_required", errors);

        if (definition.Inputs.Count == 0)
            errors.Add("inputs_required");
        if (definition.Outputs.Count == 0)
            errors.Add("outputs_required");
        if (definition.Modules.Count == 0)
            errors.Add("modules_required");
        if (definition.ControlFlow is null)
            errors.Add("control_flow_required");
        if (definition.Evaluators.Count == 0)
            errors.Add("evaluators_required");
        if (definition.OptimizerPolicy is null)
            errors.Add("optimizer_policy_required");
        if (definition.Provenance is null)
            errors.Add("provenance_required");

        foreach (var field in definition.Inputs.Concat(definition.Outputs))
        {
            Require(field.Name, "field_name_required", errors);
            Require(field.TypeRef, "field_type_ref_required", errors);
            RejectSecretPayload(field.SourceRef, "field_source_ref_contains_secret_payload", errors);
            foreach (var validationRef in field.ValidationRefs)
                RejectSecretPayload(validationRef, "field_validation_ref_contains_secret_payload", errors);
        }

        foreach (var module in definition.Modules)
        {
            Require(module.ModuleId, "module_id_required", errors);
            RejectSecretPayload(module.ModelRoutePolicyRef, "model_route_policy_ref_contains_secret_payload", errors);
            RejectSecretPayload(module.RetrievalPolicyRef, "retrieval_policy_ref_contains_secret_payload", errors);
            RejectSecretPayload(module.ToolsetRef, "toolset_ref_contains_secret_payload", errors);
            RejectSecretPayload(module.MemoryPolicyRef, "memory_policy_ref_contains_secret_payload", errors);
            foreach (var skillRef in module.SkillRefs)
                RejectSecretPayload(skillRef, "skill_ref_contains_secret_payload", errors);
            foreach (var componentRef in module.PromptComponents.Concat(module.ComponentRefs))
                ValidateComponentRef(componentRef, errors);
        }

        if (definition.ControlFlow is not null)
        {
            if (definition.ControlFlow.ModuleOrder.Count == 0)
                errors.Add("control_flow_module_order_required");
            foreach (var componentRef in definition.ControlFlow.ControlComponentRefs)
                ValidateComponentRef(componentRef, errors);
        }

        foreach (var evaluator in definition.Evaluators)
        {
            Require(evaluator.MetricId, "evaluator_metric_id_required", errors);
            RejectSecretPayload(evaluator.DatasetSplitId, "evaluator_dataset_split_contains_secret_payload", errors);
            RejectSecretPayload(evaluator.ScorerRef, "evaluator_scorer_ref_contains_secret_payload", errors);
            RejectSecretPayload(evaluator.CostBudgetRef, "evaluator_cost_budget_ref_contains_secret_payload", errors);
        }

        if (definition.OptimizerPolicy is not null)
        {
            Require(definition.OptimizerPolicy.PolicyId, "optimizer_policy_id_required", errors);
            if (definition.OptimizerPolicy.AllowedStrategyIds.Count == 0)
                errors.Add("optimizer_allowed_strategies_required");
            if (definition.OptimizerPolicy.MaxCostUsd <= 0)
                errors.Add("optimizer_max_cost_required");
            if (definition.OptimizerPolicy.MaxRollouts <= 0)
                errors.Add("optimizer_max_rollouts_required");
            RejectSecretPayload(definition.OptimizerPolicy.DatasetSplitId, "optimizer_dataset_split_contains_secret_payload", errors);
            RejectSecretPayload(definition.OptimizerPolicy.PromotionGateRef, "optimizer_promotion_gate_contains_secret_payload", errors);
            RejectSecretPayload(definition.OptimizerPolicy.RollbackPolicyRef, "optimizer_rollback_policy_contains_secret_payload", errors);
        }

        if (definition.Provenance is not null)
        {
            if (definition.Provenance.SourceStoryIds.Count == 0)
                errors.Add("provenance_source_story_required");
            if (definition.Provenance.SourceDocumentRefs.Count == 0)
                errors.Add("provenance_source_document_required");
            if (string.IsNullOrWhiteSpace(definition.Provenance.LicensePosture))
                errors.Add("provenance_license_posture_required");
            foreach (var reference in definition.Provenance.SourceDocumentRefs.Concat(definition.Provenance.ExternalInspirationRefs))
                RejectSecretPayload(reference, "provenance_ref_contains_secret_payload", errors);
        }

        return new AgentProgramValidationResult(errors.Count == 0, errors);
    }

    public AgentProgramValidationResult Validate(AgentCompiledVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        var errors = new List<string>();

        Require(variant.VariantId, "variant_id_required", errors);
        Require(variant.SourceProgramId, "source_program_id_required", errors);
        Require(variant.SourceProgramVersion, "source_program_version_required", errors);
        Require(variant.CompileStrategyId, "compile_strategy_id_required", errors);
        if (variant.FrozenComponentRefs.Count == 0)
            errors.Add("frozen_component_refs_required");
        if (variant.EvaluatorRefs.Count == 0)
            errors.Add("variant_evaluator_refs_required");
        if (variant.EvidenceRefs.Count == 0)
            errors.Add("variant_evidence_refs_required");
        if (variant.BudgetSummaryRefs.Count == 0)
            errors.Add("variant_budget_summary_refs_required");
        if (variant.RollbackRef is null)
            errors.Add("variant_rollback_ref_required");

        foreach (var componentRef in variant.FrozenComponentRefs)
            ValidateComponentRef(componentRef, errors);
        foreach (var evidenceRef in variant.EvidenceRefs.Concat(variant.BudgetSummaryRefs))
            RejectSecretPayload(evidenceRef, "variant_ref_contains_secret_payload", errors);

        return new AgentProgramValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateComponentRef(AgentProgramComponentRef componentRef, List<string> errors)
    {
        Require(componentRef.Ref, "component_ref_required", errors);
        Require(componentRef.Purpose, "component_purpose_required", errors);
        RejectSecretPayload(componentRef.Ref, "component_ref_contains_secret_payload", errors);
        RejectSecretPayload(componentRef.Version, "component_version_contains_secret_payload", errors);
        RejectSecretPayload(componentRef.Purpose, "component_purpose_contains_secret_payload", errors);
    }

    private static void Require(string? value, string error, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(error);
    }

    private static void RejectSecretPayload(string? value, string error, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (SecretPayloadMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            errors.Add(error);
    }
}
