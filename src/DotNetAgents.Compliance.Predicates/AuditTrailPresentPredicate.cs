// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Compliance.Predicates;

public sealed class AuditTrailPresentPredicate : CompliancePredicate
{
    public override string PredicateId => "AuditTrailPresentPredicate";

    public override CompliancePredicateResult Evaluate(CompliancePredicateContext context)
    {
        var text = ReadAllText(context.Payload);
        var hasAuditEvidence =
            text.Contains("audit", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("evidenceLinks", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("workflowRunIds", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("testRunIds", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("closedByActorId", StringComparison.OrdinalIgnoreCase);

        return hasAuditEvidence
            ? CompliancePredicateResult.Pass("Audit/evidence trail marker present.", "audit-trail")
            : CompliancePredicateResult.Fail("No audit/evidence trail marker found.", "audit-trail-missing");
    }
}
