// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

internal static class McpStreamableHttpLearningPublisher
{
    public static async Task PublishForToolCallAsync(
        ILessonEventPublisher publisher,
        string service,
        string tool,
        string correlationId,
        McpToolCallResponse response,
        long durationMs,
        CancellationToken cancellationToken)
    {
        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;
        var outcome = response.Success ? "completed" : "failed";
        var problemSignature = response.Success
            ? null
            : $"{service}:{tool}:{response.ErrorCode ?? "UNKNOWN"}";
        var lessonSummary = response.Success
            ? $"Tool {tool} completed successfully."
            : $"Tool {tool} failed with {response.ErrorCode ?? "UNKNOWN"}.";
        var confidence = response.Success ? 0.9 : 0.3;

        var lesson = new LessonEvent(
            EventId: eventId,
            OccurredAtUtc: occurredAt,
            CorrelationId: correlationId,
            Service: service,
            Step: tool,
            Outcome: outcome,
            LessonSummary: lessonSummary,
            ProblemSignature: problemSignature,
            ErrorCode: response.ErrorCode,
            Confidence: confidence);

        var learningEvent = new DnaLearningEvent
        {
            EventId = eventId,
            Timestamp = occurredAt,
            OccurredAtUtc = occurredAt,
            CorrelationId = correlationId,
            SourceService = service,
            Service = service,
            WorkflowId = "mcp.streamable.tools.call",
            Step = tool,
            Intent = tool,
            Outcome = outcome,
            DurationMs = durationMs,
            TimeCostMs = durationMs,
            ProblemSignature = problemSignature ?? string.Empty,
            LessonSummary = lessonSummary,
            ErrorCode = response.ErrorCode,
            ErrorMessage = response.Error,
            Confidence = confidence
        };

        await publisher.PublishAsync(lesson, cancellationToken).ConfigureAwait(false);
        await publisher.PublishLearningEventAsync(learningEvent, cancellationToken).ConfigureAwait(false);
    }
}
