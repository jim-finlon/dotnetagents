using Microsoft.Extensions.Logging;

namespace DotNetAgents.Security.Secrets;

/// <summary>
/// Implementation of <see cref="ISecretsProvider"/> that retrieves secrets from environment variables.
/// </summary>
public class EnvironmentSecretsProvider : ISecretsProvider
{
    private readonly ILogger<EnvironmentSecretsProvider>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentSecretsProvider"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for tracking operations.</param>
    public EnvironmentSecretsProvider(ILogger<EnvironmentSecretsProvider>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        var value = Environment.GetEnvironmentVariable(key);

        if (value == null)
        {
            _logger?.LogWarning("Secret not found in environment variables. Key: {Key}", key);
        }
        else
        {
            _logger?.LogDebug("Secret retrieved from environment variables. Key: {Key}", key);
        }

        return Task.FromResult<string?>(value);
    }

    /// <inheritdoc/>
    public Task<Dictionary<string, string?>> GetSecretsAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));

        cancellationToken.ThrowIfCancellationRequested();

        var result = new Dictionary<string, string?>();
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var value = Environment.GetEnvironmentVariable(key);
            result[key] = value;
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<bool> SecretExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();

        var exists = Environment.GetEnvironmentVariable(key) != null;
        return Task.FromResult(exists);
    }
}
