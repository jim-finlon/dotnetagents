// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Channels;

public sealed record ChannelCapabilities(
    bool SupportsText = true,
    bool SupportsMedia = false,
    bool SupportsVoiceNotes = false,
    bool SupportsThreading = false);

public sealed record ChannelMessage(
    string Channel,
    string SenderId,
    string SessionId,
    string Text,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ChannelSendResult(bool Success, string? ProviderMessageId = null, string? Error = null);

public interface IChannelAdapter
{
    string ChannelName { get; }
    ChannelCapabilities Capabilities { get; }
    Task<ChannelSendResult> SendAsync(ChannelMessage message, CancellationToken cancellationToken = default);
}

/// <summary>Optional: report adapter health for operator dashboards and runbooks.</summary>
public interface IChannelHealthProbe
{
    Task<ChannelHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public sealed record ChannelHealthResult(bool Healthy, string? Message = null, IReadOnlyDictionary<string, object>? Details = null);

/// <summary>Optional: validate webhook payload signature (e.g. Telegram, Discord). Adapters can use this before accepting ingress.</summary>
public interface IWebhookSignatureValidator
{
    string ChannelName { get; }
    bool Validate(string rawBody, string signatureHeaderValue, string secret, out string? failureReason);
}

/// <summary>Optional: provide per-channel authentication (e.g. tokens, account credentials). Adapters that need channel-specific auth can depend on this.</summary>
public interface IChannelAuthProvider
{
    string ChannelName { get; }
    /// <summary>Get a secret or token for this channel (e.g. bot token, webhook secret). Returns null if not configured.</summary>
    ValueTask<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default);
}

public interface IChannelAdapterRegistry
{
    void Register(IChannelAdapter adapter);
    IChannelAdapter? GetAdapter(string channelName);
    IReadOnlyList<IChannelAdapter> GetAdapters();
}

public sealed class InMemoryChannelAdapterRegistry : IChannelAdapterRegistry
{
    private readonly Dictionary<string, IChannelAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IChannelAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _adapters[adapter.ChannelName] = adapter;
    }

    public IChannelAdapter? GetAdapter(string channelName)
    {
        if (string.IsNullOrWhiteSpace(channelName))
        {
            return null;
        }

        return _adapters.TryGetValue(channelName, out var adapter) ? adapter : null;
    }

    public IReadOnlyList<IChannelAdapter> GetAdapters() => _adapters.Values.ToList().AsReadOnly();
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAgentsChannels(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IChannelAdapterRegistry, InMemoryChannelAdapterRegistry>();
        return services;
    }

    public static IServiceCollection AddExternalConnectorRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IExternalConnectorRegistry, InMemoryExternalConnectorRegistry>();
        return services;
    }
}
