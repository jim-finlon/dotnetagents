namespace DotNetAgents.Abstractions.PublicSubstitutes.Session;

/// <summary>
/// Stores small public-agent session snapshots. Implementations may be
/// in-memory, local file-backed, premium, or private-factory backed.
/// </summary>
public interface IPublicSessionStore
{
    ValueTask<SessionSnapshot?> LoadAsync(SessionId id, CancellationToken cancellationToken = default);

    ValueTask SaveAsync(SessionId id, SessionSnapshot snapshot, CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(SessionId id, CancellationToken cancellationToken = default);

    IAsyncEnumerable<SessionId> ListAsync(CancellationToken cancellationToken = default);
}
