using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Server.Adapters;

/// <summary>
/// Bridges <see cref="ILessonEventPublisher.PublishLearningEventAsync"/> events emitted by the
/// MCP server (on every tool call) to <see cref="IAgentLearningProjector"/>, which forwards to
/// configured HTTP targets (KnowledgeMemory, EvaluationSandbox endpoints, etc.). This is the systemic link
/// that makes dna.learning.event.v1 auto-emission actually reach downstream sinks without
/// per-service wiring.
///
/// Registered automatically by <c>ServiceCollectionExtensions.AddMcpLifecycleHooks()</c> when an
/// <see cref="IAgentLearningProjector"/> is present in the container; otherwise the default
/// <see cref="NoOpLessonEventPublisher"/> remains in effect.
///
/// The legacy <see cref="ILessonEventPublisher.PublishAsync(LessonEvent, CancellationToken)"/>
/// path is intentionally a no-op here — the <see cref="DnaLearningEvent"/> envelope is the
/// richer, canonical payload and already carries everything the legacy record conveyed.
/// </summary>
public sealed class ProjectingLessonEventPublisher(
    IAgentLearningProjector projector,
    ILogger<ProjectingLessonEventPublisher> logger) : ILessonEventPublisher
{
    public Task PublishAsync(LessonEvent lessonEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task PublishLearningEventAsync(DnaLearningEvent learningEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await projector.ProjectAsync(learningEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Projection is best-effort — never let observability failures surface as tool-call failures.
            logger.LogWarning(
                ex,
                "Failed to project MCP learning event service={Service} step={Step} correlation={CorrelationId}",
                learningEvent.Service,
                learningEvent.Step,
                learningEvent.CorrelationId);
        }
    }
}
