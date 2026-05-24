// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.SessionPersistence.Models;

namespace DotNetAgents.SessionPersistence;

/// <summary>
/// Client for the AI Session Persistence REST API. Use for shared session state,
/// tasks, lessons, and bootstrap/resume across DNA agents and handoffs.
/// </summary>
public interface ISessionPersistenceClient
{
    /// <summary>Create a new session (project). One per workstream or mission.</summary>
    Task<SessionResponse?> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Get session by id.</summary>
    Task<SessionResponse?> GetSessionAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Update session (resume point, status, metadata).</summary>
    Task<SessionResponse?> UpdateSessionAsync(Guid projectId, UpdateSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Get full bootstrap payload for resumption (JSON string). Use format: json, cursorrules, or agent.</summary>
    Task<string> GetBootstrapAsync(Guid projectId, string format = "json", bool includeGlobalLessons = true, int maxLessons = 10, CancellationToken cancellationToken = default);

    /// <summary>Get bootstrap as strongly-typed object (format=json only).</summary>
    Task<BootstrapPayload?> GetBootstrapPayloadAsync(Guid projectId, bool includeGlobalLessons = true, int maxLessons = 10, CancellationToken cancellationToken = default);

    /// <summary>List sessions (projects) for a workspace.</summary>
    Task<ListSessionsResponse?> ListSessionsAsync(string? workspaceId = null, string? status = null, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>Add a task to a project.</summary>
    Task<TaskResponse?> AddTaskAsync(Guid projectId, AddTaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>Update a task (status, notes, etc.).</summary>
    Task<TaskResponse?> UpdateTaskAsync(Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>Mark a task as completed.</summary>
    Task<TaskResponse?> CompleteTaskAsync(Guid projectId, Guid taskId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>Update session context (recent files, commit, decisions, questions).</summary>
    Task<SessionContextResponse?> UpdateSessionContextAsync(Guid projectId, UpdateSessionContextRequest request, CancellationToken cancellationToken = default);

    /// <summary>Add a lesson (project-specific or global when projectId is null).</summary>
    Task<LessonResponse?> AddLessonAsync(AddLessonRequest request, CancellationToken cancellationToken = default);

    /// <summary>Query lessons (optionally by project, category, tags, search).</summary>
    Task<QueryLessonsResponse?> QueryLessonsAsync(Guid? projectId = null, string? category = null, string? tags = null, string? search = null, bool includeGlobal = true, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>Record success/failure feedback for a lesson (updates confidence).</summary>
    Task RecordLessonFeedbackAsync(Guid lessonId, bool success, CancellationToken cancellationToken = default);

    /// <summary>Create a snapshot of current session state.</summary>
    Task<SnapshotResponse?> CreateSnapshotAsync(Guid projectId, string trigger = "Manual", string? triggerDetails = null, CancellationToken cancellationToken = default);

    /// <summary>Get project rules (coding standards, commit conventions, etc.).</summary>
    Task<ProjectRulesResponse?> GetProjectRulesAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>Update or create project rules.</summary>
    Task<ProjectRulesResponse?> UpdateProjectRulesAsync(Guid projectId, UpdateProjectRulesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Health check (GET /health).</summary>
    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}
