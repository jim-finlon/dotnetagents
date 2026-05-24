// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Runtime;

public enum AsiSubjectKind
{
    AgentProgram,
    ProgramModule,
    ProgramComponent,
    PromptGene,
    Skill,
    ToolDescription,
    RoutingPolicy,
    EvaluatorRubric
}

public enum AsiPrivacyClass
{
    Unclassified,
    Public,
    Internal,
    Restricted,
    PrivateTrace,
    SecretReferenceOnly
}

public sealed record ComponentTargetRef(
    OptimizerComponentKind Kind,
    string Ref,
    string? ProgramId = null,
    string? ModuleId = null,
    string? ComponentId = null);

public sealed record ComponentFeedback(
    string FeedbackId,
    AsiSubjectKind SubjectKind,
    string SubjectId,
    string EvaluatorId,
    string Feedback,
    ComponentTargetRef TargetComponent,
    IReadOnlyList<string> EvidenceRefs,
    string? ProgramId = null,
    string? ModuleId = null,
    string? ComponentId = null);

public sealed record EvidenceClassificationRef(
    OptimizerReceiptEvidenceClass EvidenceClass,
    IReadOnlyList<string> LocalEvidenceRefs,
    IReadOnlyList<string> ExternalClaimRefs,
    bool ContainsPrivateTrace = false);

public sealed record ActionableSideInformation
{
    public string AsiId { get; init; } = Guid.NewGuid().ToString("n");
    public ComponentFeedback Feedback { get; init; } = new(
        string.Empty,
        AsiSubjectKind.ProgramComponent,
        string.Empty,
        string.Empty,
        string.Empty,
        new ComponentTargetRef(OptimizerComponentKind.PromptGene, string.Empty),
        []);
    public AsiPrivacyClass PrivacyClass { get; init; } = AsiPrivacyClass.Unclassified;
    public EvidenceClassificationRef EvidenceClassification { get; init; } = new(
        OptimizerReceiptEvidenceClass.ExternalClaim,
        [],
        []);
    public IReadOnlyList<string> ConstraintRefs { get; init; } = [];
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record DatasetSplitPolicy(
    string DatasetId,
    int TrainCount,
    int ValidationCount,
    int TestCount,
    string SplitPolicy,
    string HeldOutSplitId);

public sealed record HeldOutReplayResult(
    string ReplayId,
    string HeldOutSplitId,
    double BaselineScore,
    double CandidateScore,
    IReadOnlyList<string> RegressionNotes,
    IReadOnlyList<string> EvidenceRefs);

public sealed record ComponentUpdateProposal(
    string ProposalId,
    string AsiId,
    ComponentTargetRef TargetComponent,
    string ProposedArtifactRef,
    bool PromoteToShared,
    RollbackRef? RollbackRef,
    bool AllowRegressionOverride = false);

public sealed record PromotionGateDecision(
    bool Allowed,
    IReadOnlyList<string> Errors);

public sealed class ComponentScopedAsiValidator
{
    public PromotionGateDecision Validate(ActionableSideInformation asi)
    {
        ArgumentNullException.ThrowIfNull(asi);
        var errors = new List<string>();

        Require(asi.AsiId, "asi_id_required", errors);
        ValidateFeedback(asi.Feedback, errors);

        if (asi.PrivacyClass == AsiPrivacyClass.Unclassified)
            errors.Add("privacy_class_required");

        if (asi.EvidenceClassification.EvidenceClass == OptimizerReceiptEvidenceClass.ExternalClaim &&
            asi.EvidenceClassification.ExternalClaimRefs.Count == 0)
            errors.Add("external_claim_ref_required");

        if (asi.EvidenceClassification.EvidenceClass != OptimizerReceiptEvidenceClass.ExternalClaim &&
            asi.EvidenceClassification.LocalEvidenceRefs.Count == 0)
            errors.Add("local_evidence_ref_required");

        return new PromotionGateDecision(errors.Count == 0, errors);
    }

    private static void ValidateFeedback(ComponentFeedback feedback, ICollection<string> errors)
    {
        Require(feedback.FeedbackId, "feedback_id_required", errors);
        Require(feedback.SubjectId, "subject_id_required", errors);
        Require(feedback.EvaluatorId, "evaluator_id_required", errors);
        Require(feedback.Feedback, "feedback_required", errors);
        ValidateTarget(feedback.TargetComponent, errors);
        if (feedback.EvidenceRefs.Count == 0)
            errors.Add("feedback_evidence_refs_required");
    }

    internal static void ValidateTarget(ComponentTargetRef target, ICollection<string> errors)
    {
        Require(target.Ref, "target_component_ref_required", errors);
        if (target.Kind is OptimizerComponentKind.AgentProgram &&
            string.IsNullOrWhiteSpace(target.ProgramId))
            errors.Add("target_program_id_required");
    }

    internal static void Require(string? value, string error, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(error);
    }
}

public sealed class ComponentScopedPromotionGate
{
    private readonly ComponentScopedAsiValidator _asiValidator = new();

    public PromotionGateDecision Evaluate(
        ActionableSideInformation asi,
        ComponentUpdateProposal proposal,
        DatasetSplitPolicy? splitPolicy,
        HeldOutReplayResult? heldOutReplay)
    {
        ArgumentNullException.ThrowIfNull(asi);
        ArgumentNullException.ThrowIfNull(proposal);
        var errors = new List<string>(_asiValidator.Validate(asi).Errors);

        ValidateProposal(asi, proposal, errors);
        ValidateSplitPolicy(splitPolicy, errors);
        ValidateHeldOutReplay(proposal, splitPolicy, heldOutReplay, errors);
        ValidatePrivacyForSharedPromotion(asi, proposal, errors);
        ValidateEvidenceBoundary(asi, errors);

        return new PromotionGateDecision(errors.Count == 0, errors);
    }

    private static void ValidateProposal(
        ActionableSideInformation asi,
        ComponentUpdateProposal proposal,
        ICollection<string> errors)
    {
        ComponentScopedAsiValidator.Require(proposal.ProposalId, "proposal_id_required", errors);
        ComponentScopedAsiValidator.Require(proposal.AsiId, "proposal_asi_id_required", errors);
        ComponentScopedAsiValidator.Require(proposal.ProposedArtifactRef, "proposed_artifact_ref_required", errors);
        ComponentScopedAsiValidator.ValidateTarget(proposal.TargetComponent, errors);

        if (!string.Equals(proposal.AsiId, asi.AsiId, StringComparison.Ordinal))
            errors.Add("proposal_asi_mismatch");
        if (proposal.RollbackRef is null)
            errors.Add("rollback_ref_required_for_promotion");
    }

    private static void ValidateSplitPolicy(DatasetSplitPolicy? splitPolicy, ICollection<string> errors)
    {
        if (splitPolicy is null)
        {
            errors.Add("dataset_split_policy_required");
            return;
        }

        ComponentScopedAsiValidator.Require(splitPolicy.DatasetId, "dataset_id_required", errors);
        ComponentScopedAsiValidator.Require(splitPolicy.SplitPolicy, "split_policy_required", errors);
        ComponentScopedAsiValidator.Require(splitPolicy.HeldOutSplitId, "held_out_split_id_required", errors);
        if (splitPolicy.TrainCount <= 0)
            errors.Add("train_split_required");
        if (splitPolicy.ValidationCount <= 0)
            errors.Add("validation_split_required");
        if (splitPolicy.TestCount <= 0)
            errors.Add("test_split_required");
    }

    private static void ValidateHeldOutReplay(
        ComponentUpdateProposal proposal,
        DatasetSplitPolicy? splitPolicy,
        HeldOutReplayResult? heldOutReplay,
        ICollection<string> errors)
    {
        if (heldOutReplay is null)
        {
            errors.Add("held_out_replay_required_for_promotion");
            return;
        }

        ComponentScopedAsiValidator.Require(heldOutReplay.ReplayId, "held_out_replay_id_required", errors);
        if (heldOutReplay.EvidenceRefs.Count == 0)
            errors.Add("held_out_replay_evidence_required");
        if (splitPolicy is not null &&
            !string.Equals(heldOutReplay.HeldOutSplitId, splitPolicy.HeldOutSplitId, StringComparison.Ordinal))
            errors.Add("held_out_split_mismatch");
        if (heldOutReplay.CandidateScore < heldOutReplay.BaselineScore)
            errors.Add("held_out_score_regression");
        if (heldOutReplay.RegressionNotes.Count > 0 && !proposal.AllowRegressionOverride)
            errors.Add("regression_notes_require_explicit_override");
    }

    private static void ValidatePrivacyForSharedPromotion(
        ActionableSideInformation asi,
        ComponentUpdateProposal proposal,
        ICollection<string> errors)
    {
        if (!proposal.PromoteToShared)
            return;

        if (asi.PrivacyClass is AsiPrivacyClass.Unclassified or AsiPrivacyClass.PrivateTrace or AsiPrivacyClass.SecretReferenceOnly)
            errors.Add("shared_promotion_requires_classified_non_private_asi");
        if (asi.EvidenceClassification.ContainsPrivateTrace)
            errors.Add("private_trace_cannot_back_shared_promotion");
    }

    private static void ValidateEvidenceBoundary(ActionableSideInformation asi, ICollection<string> errors)
    {
        if (asi.EvidenceClassification.EvidenceClass == OptimizerReceiptEvidenceClass.ExternalClaim &&
            asi.EvidenceClassification.LocalEvidenceRefs.Count == 0)
            errors.Add("external_claim_requires_local_evidence_before_promotion");
    }
}
