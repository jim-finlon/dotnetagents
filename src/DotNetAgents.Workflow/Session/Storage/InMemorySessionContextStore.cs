using DotNetAgents.Workflow.Session;

namespace DotNetAgents.Workflow.Session.Storage;

/// <summary>
/// In-memory implementation of <see cref="ISessionContextStore"/> for testing and development.
/// </summary>
public class InMemorySessionContextStore : ISessionContextStore
{
    private readonly Dictionary<string, SessionContext> _contexts = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task<SessionContext> CreateOrUpdateAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (string.IsNullOrWhiteSpace(context.SessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(context));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var existingContext = _contexts.TryGetValue(context.SessionId, out var existing) ? existing : null;

            var contextToStore = context with
            {
                Id = existingContext?.Id ?? (context.Id == default ? Guid.NewGuid() : context.Id),
                CreatedAt = existingContext?.CreatedAt ?? (context.CreatedAt == default ? now : context.CreatedAt),
                UpdatedAt = now
            };

            _contexts[contextToStore.SessionId] = contextToStore;
            return Task.FromResult(contextToStore);
        }
    }

    /// <inheritdoc/>
    public Task<SessionContext?> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _contexts.TryGetValue(sessionId, out var context);
            return Task.FromResult<SessionContext?>(context);
        }
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _contexts.Remove(sessionId);
        }

        return Task.CompletedTask;
    }
}
