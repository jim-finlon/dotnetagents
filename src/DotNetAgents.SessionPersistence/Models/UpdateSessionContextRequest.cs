// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record UpdateSessionContextRequest(
    IReadOnlyList<string>? RecentFiles = null,
    string? LastModifiedFile = null,
    string? LastCommitMessage = null,
    string? LastCommitHash = null,
    IReadOnlyList<string>? KeyDecisions = null,
    IReadOnlyList<string>? OpenQuestions = null,
    Dictionary<string, string>? Assumptions = null,
    IReadOnlyList<string>? RecentCommands = null,
    IReadOnlyList<string>? RecentErrors = null,
    string? WorkingDirectory = null,
    string? ActiveBranch = null
);
