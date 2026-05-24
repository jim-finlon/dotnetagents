// SPDX-License-Identifier: Apache-2.0

using System.Collections.ObjectModel;

namespace DotNetAgents.Runtime;

public enum OptimizerStrategyKind
{
    FewShotBootstrap,
    KnnFewShot,
    InstructionSearch,
    ReflectiveParetoEvolution,
    SimbaRuleDemo,
    TextualFeedbackUpdate,
    EnsembleCompile,
    FineTuneHandoff
}

public enum OptimizerComponentKind
{
    PromptGene,
    Skill,
    ToolDescription,
    RoutingPolicy,
    AgentProgram,
    EvaluatorRubric,
    RuntimeAssemblyRule
}

public enum OptimizerReceiptEvidenceClass
{
    ExternalClaim,
    LocalValidationHarness,
    EmpiricalBenchmark
}

public enum OptimizerPromotionOutcome
{
    Promote,
    Reject,
    NeedsReview
}

public sealed record OptimizerStrategyDescriptor(
    string StrategyId,
    OptimizerStrategyKind Kind,
    string DisplayName,
    IReadOnlySet<OptimizerComponentKind> SupportedComponentKinds,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> BudgetDimensions,
    bool RequiresHeldOutReplay,
    bool CanProduceFineTuneData = false);

public sealed record OptimizerBudgetPolicy(
    int MaxModelCalls,
    decimal MaxReflectionCostUsd,
    decimal MaxTaskModelCostUsd,
    int MaxCandidateCount,
    TimeSpan MaxWallClock,
    int MaxPromptGrowthCharacters);

public sealed record DatasetSplitManifest(
    string DatasetId,
    int TrainCount,
    int ValidationCount,
    int TestCount,
    string SplitPolicy);

public sealed record CandidateLineageEntry(
    string CandidateId,
    string? ParentCandidateId,
    string MutationKind,
    IReadOnlyList<string> SourceComponentRefs,
    IReadOnlyList<string> EvidenceRefs);

public sealed record CostLedgerEntry(
    string Category,
    int ModelCalls,
    decimal EstimatedCostUsd);

public sealed record OptimizerScoreDelta(
    double BaselineScore,
    double CandidateScore,
    double Delta);

public sealed record RollbackRef(
    string VariantRef,
    string Reason);

public sealed record PromotionRecommendation(
    OptimizerPromotionOutcome Outcome,
    string Rationale,
    OptimizerScoreDelta ScoreDelta,
    bool HeldOutReplayPassed,
    IReadOnlyList<string> FailureNotes,
    IReadOnlyList<string> RegressionNotes,
    RollbackRef? RollbackRef);

public sealed record OptimizerRunReceipt
{
    public string RunId { get; init; } = Guid.NewGuid().ToString("n");
    public string StrategyId { get; init; } = string.Empty;
    public string SourceProgramRef { get; init; } = string.Empty;
    public IReadOnlyList<string> SourceComponentRefs { get; init; } = [];
    public DatasetSplitManifest? DatasetSplit { get; init; }
    public OptimizerBudgetPolicy? BudgetPolicy { get; init; }
    public IReadOnlyList<CandidateLineageEntry> CandidateLineage { get; init; } = [];
    public IReadOnlyList<CostLedgerEntry> CostLedger { get; init; } = [];
    public PromotionRecommendation? PromotionRecommendation { get; init; }
    public OptimizerReceiptEvidenceClass EvidenceClass { get; init; } = OptimizerReceiptEvidenceClass.LocalValidationHarness;
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record OptimizerReceiptValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed class OptimizerRunReceiptValidator
{
    public OptimizerReceiptValidationResult Validate(OptimizerRunReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(receipt.StrategyId))
            errors.Add("strategy_id_required");
        if (string.IsNullOrWhiteSpace(receipt.SourceProgramRef))
            errors.Add("source_program_ref_required");
        if (receipt.DatasetSplit is null)
            errors.Add("dataset_split_required");
        else
        {
            if (receipt.DatasetSplit.TrainCount <= 0)
                errors.Add("train_split_required");
            if (receipt.DatasetSplit.ValidationCount <= 0)
                errors.Add("validation_split_required");
            if (receipt.DatasetSplit.TestCount <= 0)
                errors.Add("test_split_required");
        }

        if (receipt.BudgetPolicy is null)
            errors.Add("budget_policy_required");
        else
        {
            if (receipt.BudgetPolicy.MaxModelCalls <= 0)
                errors.Add("max_model_calls_required");
            if (receipt.BudgetPolicy.MaxCandidateCount <= 0)
                errors.Add("max_candidate_count_required");
            if (receipt.BudgetPolicy.MaxWallClock <= TimeSpan.Zero)
                errors.Add("max_wall_clock_required");
        }

        if (receipt.CandidateLineage.Count == 0)
            errors.Add("candidate_lineage_required");
        if (receipt.CostLedger.Count == 0)
            errors.Add("cost_ledger_required");
        if (receipt.EvidenceRefs.Count == 0)
            errors.Add("evidence_refs_required");
        if (receipt.PromotionRecommendation is null)
            errors.Add("promotion_recommendation_required");
        else if (receipt.PromotionRecommendation.Outcome == OptimizerPromotionOutcome.Promote)
        {
            if (!receipt.PromotionRecommendation.HeldOutReplayPassed)
                errors.Add("held_out_replay_required_for_promotion");
            if (receipt.PromotionRecommendation.RegressionNotes.Count > 0)
                errors.Add("regressions_block_promotion");
            if (receipt.PromotionRecommendation.RollbackRef is null)
                errors.Add("rollback_ref_required_for_promotion");
            if (receipt.EvidenceClass == OptimizerReceiptEvidenceClass.ExternalClaim)
                errors.Add("external_claim_cannot_promote");
        }

        return new OptimizerReceiptValidationResult(errors.Count == 0, errors);
    }
}

public sealed class BuiltInOptimizerStrategyRegistry
{
    private readonly IReadOnlyDictionary<string, OptimizerStrategyDescriptor> _descriptors;

    public BuiltInOptimizerStrategyRegistry()
    {
        _descriptors = CreateBuiltIns().ToDictionary(descriptor => descriptor.StrategyId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<OptimizerStrategyDescriptor> List() =>
        _descriptors.Values.OrderBy(descriptor => descriptor.StrategyId, StringComparer.Ordinal).ToArray();

    public OptimizerStrategyDescriptor? Get(string strategyId) =>
        _descriptors.TryGetValue(strategyId, out var descriptor) ? descriptor : null;

    private static IReadOnlyList<OptimizerStrategyDescriptor> CreateBuiltIns() =>
    [
        Descriptor("few-shot-bootstrap", OptimizerStrategyKind.FewShotBootstrap, "Few-shot bootstrap", Set(OptimizerComponentKind.PromptGene, OptimizerComponentKind.AgentProgram), ["labeled examples", "metric"], ["model calls", "candidate count"], requiresHeldOut: true),
        Descriptor("knn-few-shot", OptimizerStrategyKind.KnnFewShot, "KNN few-shot", Set(OptimizerComponentKind.PromptGene, OptimizerComponentKind.RuntimeAssemblyRule), ["embedding/search corpus", "labeled examples"], ["retrieval calls", "model calls"], requiresHeldOut: true),
        Descriptor("instruction-search", OptimizerStrategyKind.InstructionSearch, "Instruction search", Set(OptimizerComponentKind.PromptGene, OptimizerComponentKind.AgentProgram), ["dataset summary", "metric", "previous scores"], ["model calls", "candidate count"], requiresHeldOut: true),
        Descriptor("reflective-pareto-evolution", OptimizerStrategyKind.ReflectiveParetoEvolution, "Reflective Pareto evolution", Set(OptimizerComponentKind.PromptGene, OptimizerComponentKind.Skill, OptimizerComponentKind.ToolDescription, OptimizerComponentKind.RoutingPolicy, OptimizerComponentKind.AgentProgram), ["trajectories", "actionable side information", "evaluator"], ["reflection cost", "task-model cost", "rollouts"], requiresHeldOut: true),
        Descriptor("simba-rule-demo", OptimizerStrategyKind.SimbaRuleDemo, "SIMBA rule/demo", Set(OptimizerComponentKind.PromptGene, OptimizerComponentKind.EvaluatorRubric), ["challenging examples", "output variability"], ["mini-batches", "model calls"], requiresHeldOut: true),
        Descriptor("textual-feedback-update", OptimizerStrategyKind.TextualFeedbackUpdate, "Textual feedback update", Set(OptimizerComponentKind.PromptGene, OptimizerComponentKind.ToolDescription, OptimizerComponentKind.RoutingPolicy, OptimizerComponentKind.EvaluatorRubric), ["component-scoped feedback", "constraints", "validation"], ["backward model calls", "update iterations"], requiresHeldOut: true),
        Descriptor("ensemble-compile", OptimizerStrategyKind.EnsembleCompile, "Ensemble compile", Set(OptimizerComponentKind.AgentProgram), ["candidate pool", "latency budget", "reducer"], ["candidate count", "inference cost"], requiresHeldOut: true),
        Descriptor("fine-tune-handoff", OptimizerStrategyKind.FineTuneHandoff, "Fine-tune handoff", Set(OptimizerComponentKind.AgentProgram), ["validated traces", "provider capability", "security approval"], ["training cost", "dataset size"], requiresHeldOut: true, canProduceFineTuneData: true)
    ];

    private static IReadOnlySet<OptimizerComponentKind> Set(params OptimizerComponentKind[] values) =>
        new HashSet<OptimizerComponentKind>(values);

    private static OptimizerStrategyDescriptor Descriptor(
        string id,
        OptimizerStrategyKind kind,
        string displayName,
        IReadOnlySet<OptimizerComponentKind> supportedKinds,
        IReadOnlyList<string> requiredEvidence,
        IReadOnlyList<string> budgetDimensions,
        bool requiresHeldOut,
        bool canProduceFineTuneData = false) =>
        new(
            id,
            kind,
            displayName,
            supportedKinds,
            requiredEvidence,
            budgetDimensions,
            requiresHeldOut,
            canProduceFineTuneData);
}
