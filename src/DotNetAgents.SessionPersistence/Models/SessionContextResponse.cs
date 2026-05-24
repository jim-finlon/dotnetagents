// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record SessionContextResponse(
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<string>? RecentFiles,
    string? LastModifiedFile,
    string? LastCommitMessage,
    string? LastCommitHash,
    IReadOnlyList<string>? KeyDecisions,
    IReadOnlyList<string>? OpenQuestions,
    Dictionary<string, string>? Assumptions,
    IReadOnlyList<string>? RecentCommands,
    IReadOnlyList<string>? RecentErrors,
    string? WorkingDirectory,
    string? ActiveBranch,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
