// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record AddLessonRequest(
    string Title,
    string Description,
    string Category,
    string? Context = null,
    string? Solution = null,
    string Severity = "Info",
    Guid? ProjectId = null,
    Guid? TaskId = null,
    IReadOnlyList<string>? Tags = null,
    string? ErrorMessage = null,
    string? ToolName = null,
    Dictionary<string, object>? ToolParameters = null
);
