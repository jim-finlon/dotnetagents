// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

/// <summary>Public task record stored by local or hosted substitute adapters.</summary>
public sealed record PublicTaskRecord(
    string Id,
    PublicTaskRequest Request,
    PublicTaskOutcome? Outcome,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);
