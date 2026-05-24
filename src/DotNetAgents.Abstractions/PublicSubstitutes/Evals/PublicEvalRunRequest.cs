// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Evals;

/// <summary>Public request to begin an eval run.</summary>
public sealed record PublicEvalRunRequest(
    string SuiteName,
    string? Subject = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    DateTimeOffset? StartedAt = null);
