using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

public sealed record McpToolCallContext(
    string Service,
    string Tool,
    IReadOnlyDictionary<string, object> Arguments,
    string CorrelationId,
    DateTimeOffset StartedAtUtc);

public sealed record McpToolLifecycleEvent(
    string Service,
    string Tool,
    string CorrelationId,
    bool Success,
    long DurationMs,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record McpSafetyVerificationResult(
    bool Allowed,
    string? ErrorCode = null,
    string? Message = null,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public static McpSafetyVerificationResult Pass(IReadOnlyDictionary<string, string>? metadata = null) => new(true, Metadata: metadata);
}

public sealed record LessonEvent(
    string EventId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string Service,
    string Step,
    string Outcome,
    string LessonSummary,
    string? ProblemSignature,
    string? ErrorCode,
    double Confidence);

public interface IMcpEventSink
{
    Task OnToolCallStartedAsync(McpToolCallContext context, CancellationToken cancellationToken = default);

    Task OnToolCallCompletedAsync(
        McpToolCallContext context,
        McpToolCallResponse response,
        McpToolLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default);
}

public interface IMcpSafetyVerifier
{
    Task<McpSafetyVerificationResult> VerifyBeforeCallAsync(McpToolCallContext context, CancellationToken cancellationToken = default);

    Task<McpSafetyVerificationResult> VerifyAfterCallAsync(
        McpToolCallContext context,
        McpToolCallResponse response,
        CancellationToken cancellationToken = default);
}

public interface ILessonEventPublisher
{
    Task PublishAsync(LessonEvent lessonEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish the canonical dna.learning.event.v1 envelope. Default implementation is a no-op
    /// so existing implementers continue to compile and new implementers can opt in to the
    /// richer envelope without breaking the older <see cref="PublishAsync(LessonEvent, CancellationToken)"/>
    /// contract. The MCP server calls both methods after every tool call; which one a concrete
    /// publisher forwards to its transport is an implementation choice.
    /// </summary>
    Task PublishLearningEventAsync(DnaLearningEvent learningEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
