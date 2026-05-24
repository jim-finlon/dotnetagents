// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record UpdateTaskRequest(
    string? Content = null,
    string? Status = null,
    string? Priority = null,
    string? Notes = null
);
