// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

/// <summary>Query shape for lightweight public task records.</summary>
public sealed record PublicTaskQuery(
    string? KindEquals = null,
    bool? CompletedOnly = null,
    int? Limit = null);
