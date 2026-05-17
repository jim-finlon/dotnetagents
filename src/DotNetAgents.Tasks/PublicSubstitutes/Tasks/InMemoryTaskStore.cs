using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

namespace DotNetAgents.Tasks.PublicSubstitutes.Tasks;

/// <summary>
/// Process-scoped public task store for examples, tests, and prototypes.
/// Records are lost when the process exits.
/// </summary>
public sealed class InMemoryTaskStore : IPublicTaskStore
{
    private readonly ConcurrentDictionary<string, PublicTaskRecord> _tasks = new(StringComparer.Ordinal);

    public ValueTask<PublicTaskHandle> StartAsync(
        PublicTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Kind))
            throw new ArgumentException("Kind is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Description is required.", nameof(request));

        var started = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid().ToString("N");
        var record = new PublicTaskRecord(id, Clone(request), Outcome: null, started, CompletedAt: null);
        _tasks[id] = record;

        return ValueTask.FromResult(new PublicTaskHandle(id, started));
    }

    public ValueTask CompleteAsync(
        PublicTaskHandle handle,
        PublicTaskOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(outcome);
        cancellationToken.ThrowIfCancellationRequested();

        _tasks.AddOrUpdate(
            handle.Id,
            _ => throw new KeyNotFoundException($"Task '{handle.Id}' was not found."),
            (_, existing) => existing with
            {
                Outcome = Clone(outcome),
                CompletedAt = DateTimeOffset.UtcNow
            });

        return ValueTask.CompletedTask;
    }

    public ValueTask<PublicTaskRecord?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(id))
            return ValueTask.FromResult<PublicTaskRecord?>(null);

        return ValueTask.FromResult(_tasks.TryGetValue(id, out var record) ? Clone(record) : null);
    }

    public async IAsyncEnumerable<PublicTaskRecord> ListAsync(
        PublicTaskQuery? query = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var limit = query?.Limit is > 0 ? Math.Min(query.Limit.Value, 100) : 100;
        var records = _tasks.Values
            .Where(record => Matches(record, query))
            .OrderByDescending(record => record.StartedAt)
            .ThenBy(record => record.Id, StringComparer.Ordinal)
            .Take(limit)
            .Select(Clone)
            .ToArray();

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return record;
            await Task.Yield();
        }
    }

    private static PublicTaskRequest Clone(PublicTaskRequest request) =>
        request with
        {
            Inputs = request.Inputs is null
                ? null
                : new Dictionary<string, string>(request.Inputs, StringComparer.OrdinalIgnoreCase)
        };

    private static PublicTaskOutcome Clone(PublicTaskOutcome outcome) =>
        outcome with
        {
            Outputs = outcome.Outputs is null
                ? null
                : new Dictionary<string, string>(outcome.Outputs, StringComparer.OrdinalIgnoreCase)
        };

    private static PublicTaskRecord Clone(PublicTaskRecord record) =>
        record with
        {
            Request = Clone(record.Request),
            Outcome = record.Outcome is null ? null : Clone(record.Outcome)
        };

    private static bool Matches(PublicTaskRecord record, PublicTaskQuery? query)
    {
        if (query is null)
            return true;

        if (!string.IsNullOrWhiteSpace(query.KindEquals) &&
            !string.Equals(record.Request.Kind, query.KindEquals.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return query.CompletedOnly is null ||
               (query.CompletedOnly.Value ? record.CompletedAt is not null : record.CompletedAt is null);
    }
}
