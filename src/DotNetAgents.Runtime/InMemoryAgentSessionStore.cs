namespace DotNetAgents.Runtime;

public sealed class InMemoryAgentSessionStore : IAgentSessionStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, AgentSession> _sessions = [];
    private readonly Dictionary<string, List<AgentMessage>> _messages = [];
    private readonly Dictionary<string, List<ToolInvocation>> _toolInvocations = [];
    private readonly Dictionary<string, List<ProviderCall>> _providerCalls = [];
    private readonly Dictionary<string, List<ContextSnapshot>> _contextSnapshots = [];

    public Task<AgentSession> CreateSessionAsync(
        AgentSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(session.ActorId))
            {
                throw new ArgumentException("Session actor id is required.", nameof(session));
            }

            if (_sessions.ContainsKey(session.Id))
            {
                throw new InvalidOperationException($"Session '{session.Id}' already exists.");
            }

            var rootSessionId = ResolveRootSessionId(session);
            var stored = session with
            {
                RootSessionId = rootSessionId,
                CreatedAtUtc = session.CreatedAtUtc == default ? DateTimeOffset.UtcNow : session.CreatedAtUtc,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            _sessions.Add(stored.Id, stored);
            _messages.Add(stored.Id, []);
            _toolInvocations.Add(stored.Id, []);
            _providerCalls.Add(stored.Id, []);
            _contextSnapshots.Add(stored.Id, []);

            return Task.FromResult(stored);
        }
    }

    public Task<AgentSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }
    }

    public Task<AgentSession> UpdateSessionStatusAsync(
        string sessionId,
        AgentSessionStatus status,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var session = GetRequiredSession(sessionId);
            var updated = session with { Status = status, UpdatedAtUtc = DateTimeOffset.UtcNow };
            _sessions[sessionId] = updated;
            return Task.FromResult(updated);
        }
    }

    public Task<AgentMessage> AppendMessageAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            GetRequiredSession(message.SessionId);
            var list = _messages[message.SessionId];
            var stored = message with { Order = message.Order == 0 ? list.Count + 1 : message.Order };
            list.Add(stored);
            return Task.FromResult(stored);
        }
    }

    public Task<ToolInvocation> AppendToolInvocationAsync(
        ToolInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            GetRequiredSession(invocation.SessionId);
            _toolInvocations[invocation.SessionId].Add(invocation);
            return Task.FromResult(invocation);
        }
    }

    public Task<ProviderCall> AppendProviderCallAsync(
        ProviderCall providerCall,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(providerCall);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            GetRequiredSession(providerCall.SessionId);
            _providerCalls[providerCall.SessionId].Add(providerCall);
            return Task.FromResult(providerCall);
        }
    }

    public Task<ContextSnapshot> AppendContextSnapshotAsync(
        ContextSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            GetRequiredSession(snapshot.SessionId);
            _contextSnapshots[snapshot.SessionId].Add(snapshot);
            return Task.FromResult(snapshot);
        }
    }

    public Task<AgentSessionActivity> ReadActivityAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var session = GetRequiredSession(sessionId);
            return Task.FromResult(new AgentSessionActivity(
                session,
                [.. _messages[sessionId]],
                [.. _toolInvocations[sessionId]],
                [.. _providerCalls[sessionId]],
                [.. _contextSnapshots[sessionId]]));
        }
    }

    private string ResolveRootSessionId(AgentSession session)
    {
        if (session.ParentSessionId is null)
        {
            return session.Id;
        }

        if (session.ParentSessionId == session.Id)
        {
            throw new InvalidOperationException("A session cannot be its own parent.");
        }

        var parent = GetRequiredSession(session.ParentSessionId);
        return parent.RootSessionId ?? parent.Id;
    }

    private AgentSession GetRequiredSession(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            throw new KeyNotFoundException($"Session '{sessionId}' was not found.");
        }

        return session;
    }
}
