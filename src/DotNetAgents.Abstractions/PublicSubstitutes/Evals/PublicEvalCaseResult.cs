// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Evals;

/// <summary>Flattened public result for one eval case.</summary>
public sealed record PublicEvalCaseResult(
    string CaseId,
    bool Passed,
    double? Score = null,
    string? Notes = null,
    TimeSpan? Duration = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
