using System.Collections.ObjectModel;

namespace DotNetAgents.Runtime;

public enum AgentRunMode
{
    Interactive,
    Compressed,
    Scheduled,
    Child,
    Delegated
}

public enum AgentSessionStatus
{
    Created,
    Running,
    Completed,
    CompletedWithToolErrors,
    Failed,
    Cancelled
}

public enum AgentMessageRole
{
    System,
    User,
    Assistant,
    Tool,
    Developer
}

public enum ToolInvocationStatus
{
    Started,
    Succeeded,
    Failed
}

public enum ProviderCallStatus
{
    Started,
    Succeeded,
    Failed
}

public sealed record ArtifactReference(
    string Uri,
    string MediaType,
    string? Sha256 = null,
    long? Length = null);

public sealed record ModelRouteMetadata(
    string Provider,
    string Model,
    string? Deployment = null,
    IReadOnlyDictionary<string, string>? Tags = null);

public sealed record AgentSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public string ActorId { get; init; } = string.Empty;
    public AgentRunMode RunMode { get; init; } = AgentRunMode.Interactive;
    public string? ParentSessionId { get; init; }
    public string? RootSessionId { get; init; }
    public string? DelegatedFromActorId { get; init; }
    public AgentSessionStatus Status { get; init; } = AgentSessionStatus.Created;
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
}

public sealed record AgentMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public string SessionId { get; init; } = string.Empty;
    public AgentMessageRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public int Order { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public ArtifactReference? ContentRef { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
}

public sealed record ToolInvocation
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public string SessionId { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public string? ToolCallId { get; init; }
    public string InputJson { get; init; } = "{}";
    public string? OutputJson { get; init; }
    public string? ErrorMessage { get; init; }
    public ToolInvocationStatus Status { get; init; } = ToolInvocationStatus.Started;
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public ArtifactReference? InputRef { get; init; }
    public ArtifactReference? OutputRef { get; init; }
}

public sealed record ProviderCall
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public string SessionId { get; init; } = string.Empty;
    public ModelRouteMetadata ModelRoute { get; init; } = new("unknown", "unknown");
    public ProviderCallStatus Status { get; init; } = ProviderCallStatus.Started;
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public string? ErrorMessage { get; init; }
    public ArtifactReference? RequestRef { get; init; }
    public ArtifactReference? ResponseRef { get; init; }
}

public sealed record ContextSnapshot
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public string SessionId { get; init; } = string.Empty;
    public int MessageCount { get; init; }
    public int ToolInvocationCount { get; init; }
    public int ProviderCallCount { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public ArtifactReference? SnapshotRef { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
}

public sealed record TrajectoryArtifact
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public string SessionId { get; init; } = string.Empty;
    public string ActorId { get; init; } = string.Empty;
    public AgentSessionStatus RunStatus { get; init; }
    public ModelRouteMetadata ModelRoute { get; init; } = new("unknown", "unknown");
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<AgentMessage> Messages { get; init; } = [];
    public IReadOnlyList<ToolInvocation> ToolInvocations { get; init; } = [];
    public IReadOnlyList<ProviderCall> ProviderCalls { get; init; } = [];
    public IReadOnlyList<ContextSnapshot> ContextSnapshots { get; init; } = [];
    public IReadOnlyList<ArtifactReference> ArtifactRefs { get; init; } = [];
}

public sealed record AgentSessionActivity(
    AgentSession Session,
    IReadOnlyList<AgentMessage> Messages,
    IReadOnlyList<ToolInvocation> ToolInvocations,
    IReadOnlyList<ProviderCall> ProviderCalls,
    IReadOnlyList<ContextSnapshot> ContextSnapshots);

public sealed record PlannedToolCall(
    string ToolName,
    object Input,
    string? ToolCallId = null);

public sealed record AgentModelRequest(
    AgentSession Session,
    IReadOnlyList<AgentMessage> Messages,
    ModelRouteMetadata ModelRoute);

public sealed record AgentModelResponse(
    string AssistantMessage,
    IReadOnlyList<PlannedToolCall>? ToolCalls = null,
    int? InputTokens = null,
    int? OutputTokens = null);

public sealed record AgentRunRequest
{
    public string ActorId { get; init; } = string.Empty;
    public string UserInput { get; init; } = string.Empty;
    public AgentRunMode RunMode { get; init; } = AgentRunMode.Interactive;
    public string? ParentSessionId { get; init; }
    public string? DelegatedFromActorId { get; init; }
    public ModelRouteMetadata ModelRoute { get; init; } = new("fake", "test-model");
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = ReadOnlyDictionary<string, string>.Empty;
}

public sealed record AgentRunResult(
    AgentSession Session,
    AgentSessionStatus Status,
    string AssistantMessage,
    TrajectoryArtifact Trajectory);
