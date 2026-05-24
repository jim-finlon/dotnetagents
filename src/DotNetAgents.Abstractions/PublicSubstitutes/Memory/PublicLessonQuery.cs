// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Memory;

/// <summary>Query shape for public lesson lookup.</summary>
public sealed record PublicLessonQuery(
    string? Namespace = null,
    IReadOnlyCollection<string>? Tags = null,
    string? Text = null,
    int? Limit = null);
