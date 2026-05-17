namespace DotNetAgents.Memory.Advanced;

public enum AgentMemoryLayer
{
    CurrentTask,
    AgentLocal,
    SharedProject,
    DurableLongTerm
}

public enum AgentMemoryLabel
{
    CriticalLesson,
    Hazard,
    AvoidPattern,
    RepeatPattern,
    NotableFailure,
    Handoff,
    Preference,
    Decision
}

public sealed record AgentMemoryRecord
{
    public string Id { get; init; } = string.Empty;
    public AgentMemoryLayer Layer { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? AgentId { get; init; }
    public string? ProjectId { get; init; }
    public string? TaskId { get; init; }
    public double Importance { get; init; } = 0.5;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public IReadOnlyList<AgentMemoryLabel> Labels { get; init; } = Array.Empty<AgentMemoryLabel>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SourceRefs { get; init; } = Array.Empty<string>();
}

public sealed record AgentMemoryQuery
{
    public string? Text { get; init; }
    public string? AgentId { get; init; }
    public string? ProjectId { get; init; }
    public string? TaskId { get; init; }
    public IReadOnlyList<AgentMemoryLayer>? Layers { get; init; }
    public IReadOnlyList<AgentMemoryLabel>? Labels { get; init; }
    public int Limit { get; init; } = 20;
}

public sealed record AgentMemoryWritebackPlan(
    IReadOnlyList<AgentMemoryLayer> WriteLayers,
    IReadOnlyList<AgentMemoryLabel> RecommendedLabels,
    bool RequiresDurableStore,
    string Rationale);

public interface ILayeredAgentMemory
{
    Task<AgentMemoryRecord> StoreAsync(AgentMemoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentMemoryRecord>> RetrieveAsync(AgentMemoryQuery query, CancellationToken cancellationToken = default);

    AgentMemoryWritebackPlan PlanWriteback(AgentMemoryRecord record);
}
