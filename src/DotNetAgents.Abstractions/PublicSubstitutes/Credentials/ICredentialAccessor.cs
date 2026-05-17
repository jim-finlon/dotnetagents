namespace DotNetAgents.Abstractions.PublicSubstitutes.Credentials;

/// <summary>
/// Temporary handle for reading a credential value. Implementations own any heap
/// copy of the secret and must zero that copy when the accessor is disposed.
/// </summary>
public interface ICredentialAccessor : IAsyncDisposable
{
    /// <summary>Credential reference resolved by this accessor.</summary>
    CredentialReference Reference { get; }

    /// <summary>
    /// Access the current secret value as a non-string view. Callers must not log,
    /// persist, or convert the view to a string.
    /// </summary>
    ValueTask<SecretView> AccessAsync(CancellationToken cancellationToken = default);
}
