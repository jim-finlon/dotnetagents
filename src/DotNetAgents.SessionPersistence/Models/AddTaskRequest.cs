// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record AddTaskRequest(
    string Content,
    string Priority = "Medium",
    int? Order = null,
    IReadOnlyList<Guid>? DependsOn = null,
    IReadOnlyList<string>? Tags = null,
    string? Notes = null
);
