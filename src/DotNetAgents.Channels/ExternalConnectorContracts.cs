using System.Text.Json;

namespace DotNetAgents.Channels;

/// <summary>
/// Declared rough rate-limit / backoff tier for outbound SaaS connectors (Integration Gateway).
/// </summary>
public enum ConnectorRateLimitClass
{
    Unknown = 0,
    /// <summary>Typical REST CRUD (e.g. single resource get).</summary>
    Standard = 1,
    /// <summary>Stricter limits (e.g. chat completions, search).</summary>
    Strict = 2,
    /// <summary>Search/list endpoints with pagination (GitHub search, Jira JQL).</summary>
    SearchHeavy = 3
}

/// <summary>A capability exposed by an external system connector (e.g. <c>issues.search</c>, <c>repos.get</c>).</summary>
public sealed record ExternalConnectorCapability(string Name, string? Description = null);

/// <summary>Result of an authenticated health probe (no sensitive payloads).</summary>
public sealed record ExternalConnectorHealthResult(
    bool Healthy,
    string? Message = null,
    IReadOnlyDictionary<string, object>? Details = null);

/// <summary>Outcome of <see cref="IExternalConnector.InvokeAsync"/>.</summary>
public sealed record ExternalConnectorResult(
    bool Success,
    JsonElement? Payload = null,
    string? ErrorCode = null,
    string? Message = null);

/// <summary>
/// Call context: caller identity, credential reference, tracing id, and optional resolved secret material
/// (populated only inside Integration Gateway after resolving <see cref="CredentialRef"/> via CredentialsAgent).
/// </summary>
public sealed record ExternalConnectorInvokeContext(
    string CallerActorId,
    string? CallerActorType,
    string CredentialRef,
    string CorrelationId,
    string? CredentialValue = null,
    IReadOnlyDictionary<string, string>? CredentialHeaders = null,
    IReadOnlyDictionary<string, string>? CredentialMetadata = null,
    DateTimeOffset? CredentialExpiresAt = null);

/// <summary>
/// Outbound integration connector (Jira, GitHub, Chat, …). Implementations live in Integration Gateway;
/// secrets are resolved via CredentialsAgent — never pass raw tokens through this abstraction.
/// </summary>
public interface IExternalConnector
{
    /// <summary>Stable id, e.g. <c>github</c>, <c>jira-cloud</c>.</summary>
    string ConnectorId { get; }

    IReadOnlyList<ExternalConnectorCapability> SupportedCapabilities { get; }

    ConnectorRateLimitClass RateLimitClass { get; }

    Task<ExternalConnectorHealthResult> CheckHealthAsync(
        ExternalConnectorInvokeContext context,
        CancellationToken cancellationToken = default);

    /// <param name="capability">Must match a <see cref="ExternalConnectorCapability.Name"/> entry.</param>
    /// <param name="arguments">Provider-specific JSON payload; null when absent.</param>
    /// <param name="context">Caller identity, credential ref, correlation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ExternalConnectorResult> InvokeAsync(
        string capability,
        JsonElement? arguments,
        ExternalConnectorInvokeContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Registry for <see cref="IExternalConnector"/> instances (Gateway host).</summary>
public interface IExternalConnectorRegistry
{
    void Register(IExternalConnector connector);
    IExternalConnector? GetConnector(string connectorId);
    IReadOnlyList<IExternalConnector> GetConnectors();
}

public sealed class InMemoryExternalConnectorRegistry : IExternalConnectorRegistry
{
    private readonly Dictionary<string, IExternalConnector> _connectors = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IExternalConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        if (string.IsNullOrWhiteSpace(connector.ConnectorId))
            throw new ArgumentException("ConnectorId is required.", nameof(connector));
        _connectors[connector.ConnectorId] = connector;
    }

    public IExternalConnector? GetConnector(string connectorId)
    {
        if (string.IsNullOrWhiteSpace(connectorId))
            return null;
        return _connectors.TryGetValue(connectorId, out var c) ? c : null;
    }

    public IReadOnlyList<IExternalConnector> GetConnectors() => _connectors.Values.ToList().AsReadOnly();
}
