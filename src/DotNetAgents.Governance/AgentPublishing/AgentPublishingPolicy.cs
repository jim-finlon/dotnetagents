// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.AgentPublishing;

public enum AgentPublishingState
{
    PersonalDraft = 0,
    SharedCandidate = 1,
    Shared = 2,
    Verified = 3,
    Retired = 4
}

public enum AgentExecutionIdentityMode
{
    CurrentInvoker = 0,
    ExplicitExecutionIdentity = 1
}

public sealed record AgentExecutionIdentityPolicy(
    AgentExecutionIdentityMode Mode,
    string? ExecutionIdentityId = null,
    IReadOnlySet<string>? RequiredInvokerScopes = null)
{
    public IReadOnlySet<string> RequiredInvokerScopes { get; init; } =
        RequiredInvokerScopes ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record AgentPublishingReviewReceipt(
    string ReceiptId,
    string ReviewerActorId,
    DateTimeOffset ReviewedAt,
    IReadOnlySet<string> ReviewedCapabilities,
    IReadOnlySet<string> ToolScopes,
    AgentExecutionIdentityPolicy ExecutionIdentityPolicy,
    string EvidenceSummary);

public sealed record AgentPublishingRecord(
    string AgentId,
    string OwnerActorId,
    AgentPublishingState State,
    IReadOnlySet<string> DeclaredCapabilities,
    IReadOnlySet<string> DeclaredToolScopes,
    AgentExecutionIdentityPolicy ExecutionIdentityPolicy,
    AgentPublishingReviewReceipt? LastReviewReceipt = null,
    DateTimeOffset? RetiredAt = null);

public sealed record AgentPublishingTransitionRequest(
    AgentPublishingRecord Agent,
    AgentPublishingState TargetState,
    string ActorId,
    DateTimeOffset RequestedAt,
    AgentPublishingReviewReceipt? ReviewReceipt = null);

public sealed record AgentInvocationRequest(
    AgentPublishingRecord Agent,
    string InvokerActorId,
    IReadOnlySet<string> InvokerScopes,
    string ResourceScope,
    string? ExecutionIdentityId = null);

public sealed record AgentPublishingDecision(
    bool Allowed,
    string Code,
    string Message,
    AgentPublishingState? NewState = null,
    string? ReceiptId = null,
    string? EffectiveActorId = null);

public static class AgentPublishingPolicyEvaluator
{
    public static AgentPublishingDecision EvaluateTransition(AgentPublishingTransitionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var agent = request.Agent;
        if (agent.State == request.TargetState)
        {
            return Allow(
                "agent_publishing.noop",
                "Agent publishing state is unchanged.",
                request.TargetState,
                agent.LastReviewReceipt?.ReceiptId);
        }

        if (agent.State == AgentPublishingState.Retired)
        {
            return Deny("agent_publishing.retired_terminal", "Retired agents cannot be promoted or reopened.");
        }

        return (agent.State, request.TargetState) switch
        {
            (AgentPublishingState.PersonalDraft, AgentPublishingState.SharedCandidate) =>
                Allow("agent_publishing.candidate", "Personal draft can be submitted for shared review.", request.TargetState),

            (AgentPublishingState.SharedCandidate, AgentPublishingState.Shared) =>
                EvaluatePromotion(request, requireVerifiedCoverage: false),

            (AgentPublishingState.SharedCandidate, AgentPublishingState.Verified) =>
                EvaluatePromotion(request, requireVerifiedCoverage: true),

            (AgentPublishingState.Shared, AgentPublishingState.Verified) =>
                EvaluatePromotion(request, requireVerifiedCoverage: true),

            (AgentPublishingState.Shared or AgentPublishingState.Verified or AgentPublishingState.SharedCandidate,
                AgentPublishingState.Retired) =>
                Allow("agent_publishing.retired", "Shared visibility is revoked while historical receipts remain.", request.TargetState),

            _ => Deny("agent_publishing.transition_denied", "Agent publishing transition is not allowed.")
        };
    }

    public static AgentPublishingDecision EvaluateInvocation(AgentInvocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var agent = request.Agent;
        if (agent.State is not (AgentPublishingState.Shared or AgentPublishingState.Verified))
        {
            return Deny("agent_publishing.not_shared", "Only shared or verified agents can be invoked through shared surfaces.");
        }

        if (!agent.DeclaredToolScopes.Contains(request.ResourceScope))
        {
            return Deny("agent_publishing.resource_scope_denied", "Requested resource scope is outside the reviewed tool scopes.");
        }

        if (agent.ExecutionIdentityPolicy.RequiredInvokerScopes.Count > 0
            && !agent.ExecutionIdentityPolicy.RequiredInvokerScopes.All(scope => request.InvokerScopes.Contains(scope)))
        {
            return Deny("agent_publishing.invoker_scope_denied", "Invoker does not carry the scopes required by the execution policy.");
        }

        if (agent.ExecutionIdentityPolicy.Mode == AgentExecutionIdentityMode.CurrentInvoker)
        {
            return Allow(
                "agent_publishing.invoker_scoped",
                "Invocation runs with the current invoker identity.",
                agent.State,
                agent.LastReviewReceipt?.ReceiptId,
                request.InvokerActorId);
        }

        if (string.IsNullOrWhiteSpace(agent.ExecutionIdentityPolicy.ExecutionIdentityId)
            || string.IsNullOrWhiteSpace(request.ExecutionIdentityId)
            || !string.Equals(
                agent.ExecutionIdentityPolicy.ExecutionIdentityId,
                request.ExecutionIdentityId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Deny(
                "agent_publishing.execution_identity_denied",
                "Invocation requires the explicit execution identity reviewed for this agent.");
        }

        return Allow(
            "agent_publishing.explicit_identity_scoped",
            "Invocation runs with the reviewed explicit execution identity.",
            agent.State,
            agent.LastReviewReceipt?.ReceiptId,
            request.ExecutionIdentityId);
    }

    private static AgentPublishingDecision EvaluatePromotion(
        AgentPublishingTransitionRequest request,
        bool requireVerifiedCoverage)
    {
        var receipt = request.ReviewReceipt;
        if (receipt is null)
        {
            return Deny("agent_publishing.review_required", "Shared and verified promotion requires a review receipt.");
        }

        var agent = request.Agent;
        if (!receipt.ReviewedCapabilities.IsSupersetOf(agent.DeclaredCapabilities))
        {
            return Deny("agent_publishing.capability_review_incomplete", "Review receipt does not cover every declared capability.");
        }

        if (!receipt.ToolScopes.IsSupersetOf(agent.DeclaredToolScopes))
        {
            return Deny("agent_publishing.tool_scope_review_incomplete", "Review receipt does not cover every declared tool scope.");
        }

        if (!ExecutionPoliciesMatch(receipt.ExecutionIdentityPolicy, agent.ExecutionIdentityPolicy))
        {
            return Deny("agent_publishing.execution_policy_mismatch", "Review receipt execution identity policy does not match the agent.");
        }

        if (requireVerifiedCoverage && string.IsNullOrWhiteSpace(receipt.EvidenceSummary))
        {
            return Deny("agent_publishing.evidence_required", "Verified promotion requires evidence summary in the review receipt.");
        }

        return Allow(
            request.TargetState == AgentPublishingState.Verified
                ? "agent_publishing.verified"
                : "agent_publishing.shared",
            "Review receipt satisfies publishing policy.",
            request.TargetState,
            receipt.ReceiptId);
    }

    private static bool ExecutionPoliciesMatch(
        AgentExecutionIdentityPolicy receiptPolicy,
        AgentExecutionIdentityPolicy agentPolicy)
    {
        return receiptPolicy.Mode == agentPolicy.Mode
               && string.Equals(
                   receiptPolicy.ExecutionIdentityId,
                   agentPolicy.ExecutionIdentityId,
                   StringComparison.OrdinalIgnoreCase)
               && receiptPolicy.RequiredInvokerScopes.SetEquals(agentPolicy.RequiredInvokerScopes);
    }

    private static AgentPublishingDecision Allow(
        string code,
        string message,
        AgentPublishingState newState,
        string? receiptId = null,
        string? effectiveActorId = null)
    {
        return new AgentPublishingDecision(true, code, message, newState, receiptId, effectiveActorId);
    }

    private static AgentPublishingDecision Deny(string code, string message)
    {
        return new AgentPublishingDecision(false, code, message);
    }
}
