// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace DotNetAgents.Compliance.Predicates;

public sealed partial class PhiDetectorPredicate : CompliancePredicate
{
    public override string PredicateId => "PhiDetectorPredicate";

    public override CompliancePredicateResult Evaluate(CompliancePredicateContext context)
    {
        var text = ReadAllText(context.Payload);
        var matched = MedicalRecordRegex().IsMatch(text) ||
            DiagnosisRegex().IsMatch(text) ||
            PatientIdentifierRegex().IsMatch(text);

        return matched
            ? CompliancePredicateResult.Fail("Potential PHI pattern detected in payload.", "phi-pattern")
            : CompliancePredicateResult.Pass("No first-pass PHI pattern detected.");
    }

    [GeneratedRegex(@"\b(MRN|medical record|patient id)\s*[:#]?\s*[A-Z0-9-]{5,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MedicalRecordRegex();

    [GeneratedRegex(@"\b(diagnosis|icd-?10|dob|date of birth)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosisRegex();

    [GeneratedRegex(@"\b(patient|member)\s+(name|number|identifier)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PatientIdentifierRegex();
}
