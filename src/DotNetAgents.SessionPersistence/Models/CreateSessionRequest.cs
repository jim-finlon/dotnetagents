// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record CreateSessionRequest(
    string Name,
    string? Description = null,
    string? WorkspaceId = null,
    string? GitRepository = null,
    string? GitBranch = null,
    Dictionary<string, string>? Metadata = null
);
