// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace DotNetAgents.Compliance.Predicates;

public sealed partial class SecretAtRestPredicate : CompliancePredicate
{
    public override string PredicateId => "SecretAtRestPredicate";

    public override CompliancePredicateResult Evaluate(CompliancePredicateContext context)
    {
        var text = ReadAllText(context.Payload);
        var matched = AwsAccessKeyRegex().IsMatch(text) ||
            GenericSecretAssignmentRegex().IsMatch(text) ||
            PrivateKeyRegex().IsMatch(text) ||
            TokenAssignmentRegex().IsMatch(text);

        return matched
            ? CompliancePredicateResult.Fail("Secret-shaped string detected in payload.", "secret-pattern")
            : CompliancePredicateResult.Pass("No first-pass secret pattern detected.");
    }

    [GeneratedRegex(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.CultureInvariant)]
    private static partial Regex AwsAccessKeyRegex();

    [GeneratedRegex(@"(?i)\b(secret|password|api[_-]?key|client[_-]?secret)\b\s*[:=]\s*['""]?[A-Za-z0-9_\-./+=]{16,}")]
    private static partial Regex GenericSecretAssignmentRegex();

    [GeneratedRegex(@"-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.CultureInvariant)]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"(?i)\b(token|bearer)\b\s*[:=]\s*['""]?[A-Za-z0-9_\-./+=]{24,}")]
    private static partial Regex TokenAssignmentRegex();
}
