using System.Collections.Concurrent;

namespace DotNetAgents.Observability.Failures;

public sealed class InMemoryAgentFailureTelemetryStore : IAgentFailureTelemetryStore
{
    private readonly ConcurrentQueue<AgentFailureEvent> _failures = new();
    private readonly ConcurrentQueue<AgentFallbackEvent> _fallbacks = new();
    private readonly TimeProvider _timeProvider;

    public InMemoryAgentFailureTelemetryStore(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public AgentFailureEvent RecordFailure(AgentFailureEvent failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        if (string.IsNullOrWhiteSpace(failure.ActorId))
            throw new ArgumentException("Actor id is required.", nameof(failure));
        if (string.IsNullOrWhiteSpace(failure.Operation))
            throw new ArgumentException("Operation is required.", nameof(failure));
        if (string.IsNullOrWhiteSpace(failure.Summary))
            throw new ArgumentException("Summary is required.", nameof(failure));

        var normalized = failure with
        {
            Id = string.IsNullOrWhiteSpace(failure.Id) ? Guid.NewGuid().ToString("n") : failure.Id,
            OccurredAtUtc = failure.OccurredAtUtc == default ? _timeProvider.GetUtcNow() : failure.OccurredAtUtc
        };
        _failures.Enqueue(normalized);
        return normalized;
    }

    public AgentFallbackEvent RecordFallback(AgentFallbackEvent fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        if (string.IsNullOrWhiteSpace(fallback.FailureEventId))
            throw new ArgumentException("Failure event id is required.", nameof(fallback));
        if (string.IsNullOrWhiteSpace(fallback.Action))
            throw new ArgumentException("Fallback action is required.", nameof(fallback));

        var normalized = fallback with
        {
            Id = string.IsNullOrWhiteSpace(fallback.Id) ? Guid.NewGuid().ToString("n") : fallback.Id,
            OccurredAtUtc = fallback.OccurredAtUtc == default ? _timeProvider.GetUtcNow() : fallback.OccurredAtUtc
        };
        _fallbacks.Enqueue(normalized);
        return normalized;
    }

    public AgentFailureTelemetrySnapshot Snapshot(int recentLimit = 100)
    {
        if (recentLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(recentLimit), "Recent limit must be positive.");

        var failures = _failures
            .OrderByDescending(failure => failure.OccurredAtUtc)
            .Take(recentLimit)
            .ToArray();
        var fallbacks = _fallbacks
            .OrderByDescending(fallback => fallback.OccurredAtUtc)
            .Take(recentLimit)
            .ToArray();

        return new AgentFailureTelemetrySnapshot(
            failures,
            fallbacks,
            BuildPatterns(failures));
    }

    private static IReadOnlyList<AgentFailurePattern> BuildPatterns(IReadOnlyList<AgentFailureEvent> failures)
        => failures
            .GroupBy(failure => $"{failure.ActorId}|{failure.Operation}|{failure.Kind}|{failure.Dependency}", StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group =>
            {
                var last = group.MaxBy(failure => failure.OccurredAtUtc)!;
                return new AgentFailurePattern(
                    group.Key,
                    group.Count(),
                    last.Kind,
                    group.Max(failure => failure.Severity),
                    last.Summary,
                    last.OccurredAtUtc);
            })
            .OrderByDescending(pattern => pattern.Count)
            .ThenByDescending(pattern => pattern.LastSeenUtc)
            .ToArray();
}
