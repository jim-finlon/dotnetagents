namespace DotNetAgents.Security.Secrets;

/// <summary>
/// Interface for retrieving secrets securely.
/// </summary>
public interface ISecretsProvider
{
    /// <summary>
    /// Gets a secret value by key.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The secret value, or null if not found.</returns>
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple secrets by their keys.
    /// </summary>
    /// <param name="keys">The secret keys.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A dictionary mapping keys to their secret values.</returns>
    Task<Dictionary<string, string?>> GetSecretsAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a secret exists.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the secret exists; otherwise, false.</returns>
    Task<bool> SecretExistsAsync(string key, CancellationToken cancellationToken = default);
}
