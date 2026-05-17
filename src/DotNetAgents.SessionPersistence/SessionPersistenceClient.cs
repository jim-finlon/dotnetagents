using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DotNetAgents.SessionPersistence.Models;

namespace DotNetAgents.SessionPersistence;

/// <summary>
/// HTTP client for the AI Session Persistence API. Use for shared session state,
/// tasks, lessons, and bootstrap across DNA agents.
/// </summary>
public sealed class SessionPersistenceClient : ISessionPersistenceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<SessionPersistenceClient> _logger;

    public SessionPersistenceClient(HttpClient http, IOptions<SessionPersistenceClientOptions> options, ILogger<SessionPersistenceClient> logger)
    {
        _http = http;
        _logger = logger;
        var o = options.Value;
        _http.BaseAddress = new Uri(o.BaseAddress.TrimEnd('/') + "/");
        _http.Timeout = o.RequestTimeout;
        if (!string.IsNullOrEmpty(o.ApiKey))
            _http.DefaultRequestHeaders.Add("X-API-Key", o.ApiKey);
    }

    public async Task<SessionResponse?> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/projects", request, JsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionResponse?> GetSessionAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/api/v1/projects/{projectId}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionResponse?> UpdateSessionAsync(Guid projectId, UpdateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>();
        if (request.ResumePoint != null) payload["resumePoint"] = request.ResumePoint;
        if (request.CurrentResumePoint != null) payload["currentResumePoint"] = request.CurrentResumePoint;
        if (request.Status != null) payload["status"] = request.Status;
        if (request.Metadata != null) payload["metadata"] = request.Metadata;
        var response = await _http.PatchAsJsonAsync($"/api/v1/projects/{projectId}", payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetBootstrapAsync(Guid projectId, string format = "json", bool includeGlobalLessons = true, int maxLessons = 10, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v1/projects/{projectId}/bootstrap?format={Uri.EscapeDataString(format)}&includeGlobalLessons={includeGlobalLessons}&maxLessons={maxLessons}";
        var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BootstrapPayload?> GetBootstrapPayloadAsync(Guid projectId, bool includeGlobalLessons = true, int maxLessons = 10, CancellationToken cancellationToken = default)
    {
        var json = await GetBootstrapAsync(projectId, "json", includeGlobalLessons, maxLessons, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<BootstrapPayload>(json, JsonOptions);
    }

    public async Task<ListSessionsResponse?> ListSessionsAsync(string? workspaceId = null, string? status = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"pageSize={limit}", "page=1" };
        if (!string.IsNullOrEmpty(workspaceId)) query.Add($"workspaceId={Uri.EscapeDataString(workspaceId)}");
        if (!string.IsNullOrEmpty(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        var response = await _http.GetAsync($"/api/v1/projects?{string.Join("&", query)}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken).ConfigureAwait(false);
        var items = new List<SessionSummary>();
        if (doc.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            foreach (var e in itemsEl.EnumerateArray())
                items.Add(JsonSerializer.Deserialize<SessionSummary>(e.GetRawText(), JsonOptions)!);
        var total = doc.TryGetProperty("totalItems", out var t) ? t.GetInt32() : items.Count;
        return new ListSessionsResponse(items, total);
    }

    public async Task<TaskResponse?> AddTaskAsync(Guid projectId, AddTaskRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/projects/{projectId}/tasks", request, JsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>();
        if (request.Content != null) payload["content"] = request.Content;
        if (request.Status != null) payload["status"] = request.Status;
        if (request.Priority != null) payload["priority"] = request.Priority;
        if (request.Notes != null) payload["notes"] = request.Notes;
        var response = await _http.PatchAsJsonAsync($"/api/v1/projects/{projectId}/tasks/{taskId}", payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TaskResponse?> CompleteTaskAsync(Guid projectId, Guid taskId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var payload = notes != null ? new { notes } : (object?)null;
        var response = await _http.PostAsJsonAsync($"/api/v1/projects/{projectId}/tasks/{taskId}/complete", payload ?? new { }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionContextResponse?> UpdateSessionContextAsync(Guid projectId, UpdateSessionContextRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PatchAsJsonAsync($"/api/v1/projects/{projectId}/context", request, JsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionContextResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LessonResponse?> AddLessonAsync(AddLessonRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/lessons", request, JsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LessonResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<QueryLessonsResponse?> QueryLessonsAsync(Guid? projectId = null, string? category = null, string? tags = null, string? search = null, bool includeGlobal = true, int limit = 10, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"pageSize={limit}", "page=1", $"includeGlobal={includeGlobal}" };
        if (projectId.HasValue) query.Add($"projectId={projectId}");
        if (!string.IsNullOrEmpty(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrEmpty(tags)) query.Add($"tags={Uri.EscapeDataString(tags)}");
        if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var response = await _http.GetAsync($"/api/v1/lessons?{string.Join("&", query)}", cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken).ConfigureAwait(false);
        var lessons = new List<LessonInfo>();
        if (doc.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            foreach (var e in itemsEl.EnumerateArray())
                lessons.Add(JsonSerializer.Deserialize<LessonInfo>(e.GetRawText(), JsonOptions)!);
        var total = doc.TryGetProperty("totalItems", out var t) ? t.GetInt32() : lessons.Count;
        return new QueryLessonsResponse(lessons, total);
    }

    public async Task RecordLessonFeedbackAsync(Guid lessonId, bool success, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/lessons/{lessonId}/feedback", new { success }, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SnapshotResponse?> CreateSnapshotAsync(Guid projectId, string trigger = "Manual", string? triggerDetails = null, CancellationToken cancellationToken = default)
    {
        var payload = new { trigger, triggerDetails };
        var response = await _http.PostAsJsonAsync($"/api/v1/projects/{projectId}/snapshots", payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SnapshotResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProjectRulesResponse?> GetProjectRulesAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/api/v1/projects/{projectId}/rules", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectRulesResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProjectRulesResponse?> UpdateProjectRulesAsync(Guid projectId, UpdateProjectRulesRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/v1/projects/{projectId}/rules", request, JsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectRulesResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync("/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Session persistence health check failed");
            return false;
        }
    }
}
