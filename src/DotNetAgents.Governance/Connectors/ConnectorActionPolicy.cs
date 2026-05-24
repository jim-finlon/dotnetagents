// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Connectors;

public enum ConnectorActionRiskTier
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum ConnectorApprovalClass
{
    None = 0,
    OperatorReview = 1,
    SecurityReview = 2,
    BreakGlass = 3
}

public sealed record ConnectorActionMetadata(
    string ConnectorId,
    string ActionId,
    ConnectorActionRiskTier RiskTier,
    ConnectorApprovalClass RequiredApprovalClass,
    IReadOnlySet<string> AllowedActorRoles,
    bool RequiresCredentialCustody,
    string AuditCategory);

public sealed record ConnectorActionApprovalRecord(
    string ApprovalId,
    string ConnectorId,
    string ActionId,
    string RequesterActorId,
    string ApproverActorId,
    ConnectorApprovalClass ApprovalClass,
    DateTimeOffset ApprovedAt,
    DateTimeOffset ExpiresAt,
    string Rationale,
    DateTimeOffset? ReviewDueAt = null);

public sealed record ConnectorActionRequest(
    string ConnectorId,
    string ActionId,
    string ActorId,
    IReadOnlySet<string> ActorRoles,
    DateTimeOffset RequestedAt,
    string? CredentialCustodyRef = null,
    string? ApprovalId = null);

public sealed record ConnectorActionDecision(
    bool Allowed,
    string Code,
    string Message,
    string? ApprovalId = null,
    string? AuditCategory = null);

public static class ConnectorActionPolicyEvaluator
{
    public static ConnectorActionDecision Evaluate(
        ConnectorActionRequest request,
        ConnectorActionMetadata metadata,
        IEnumerable<ConnectorActionApprovalRecord> approvals)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(approvals);

        if (!Matches(metadata, request.ConnectorId, request.ActionId))
        {
            return Deny("connector.action_metadata_mismatch", "Connector action metadata does not match the request.");
        }

        if (metadata.RiskTier is ConnectorActionRiskTier.High or ConnectorActionRiskTier.Critical
            && metadata.RequiredApprovalClass == ConnectorApprovalClass.None)
        {
            return Deny("connector.high_risk_requires_review", "High-risk connector actions require an explicit approval class.");
        }

        if (metadata.AllowedActorRoles.Count > 0
            && !request.ActorRoles.Any(role => metadata.AllowedActorRoles.Contains(role)))
        {
            return Deny("connector.actor_role_denied", "Actor role is not allowlisted for this connector action.");
        }

        if (metadata.RequiresCredentialCustody && string.IsNullOrWhiteSpace(request.CredentialCustodyRef))
        {
            return Deny("connector.credential_custody_required", "Connector action requires a CredentialsAgent custody reference.");
        }

        if (metadata.RequiredApprovalClass == ConnectorApprovalClass.None)
        {
            return Allow("connector.allowed_without_approval", "Connector action is allowed by metadata policy.", metadata);
        }

        var approval = approvals.FirstOrDefault(approval =>
            Matches(approval, request.ConnectorId, request.ActionId)
            && (request.ApprovalId is null
                || string.Equals(approval.ApprovalId, request.ApprovalId, StringComparison.OrdinalIgnoreCase)));

        if (approval is null)
        {
            return Deny("connector.approval_required", "Connector action requires an approval record for this scope.");
        }

        if (approval.ApprovalClass < metadata.RequiredApprovalClass)
        {
            return Deny("connector.approval_class_insufficient", "Approval record does not satisfy the required approval class.");
        }

        if (approval.ExpiresAt <= request.RequestedAt)
        {
            return Deny("connector.approval_expired", "Connector action approval is expired.");
        }

        return Allow("connector.approved", "Connector action is approved for this scope.", metadata, approval.ApprovalId);
    }

    private static ConnectorActionDecision Allow(
        string code,
        string message,
        ConnectorActionMetadata metadata,
        string? approvalId = null)
    {
        return new ConnectorActionDecision(true, code, message, approvalId, metadata.AuditCategory);
    }

    private static ConnectorActionDecision Deny(string code, string message)
    {
        return new ConnectorActionDecision(false, code, message);
    }

    private static bool Matches(ConnectorActionMetadata metadata, string connectorId, string actionId)
    {
        return string.Equals(metadata.ConnectorId, connectorId, StringComparison.OrdinalIgnoreCase)
               && string.Equals(metadata.ActionId, actionId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(ConnectorActionApprovalRecord approval, string connectorId, string actionId)
    {
        return string.Equals(approval.ConnectorId, connectorId, StringComparison.OrdinalIgnoreCase)
               && string.Equals(approval.ActionId, actionId, StringComparison.OrdinalIgnoreCase);
    }
}
