using DotNetAgents.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Gateway;

public sealed record GatewayDispatchContext(ChannelMessage Message, DateTimeOffset ReceivedAtUtc);
public sealed record GatewayDispatchResult(bool Accepted, string? Error = null);

public interface IGatewayPolicy
{
    string Name { get; }
    Task<bool> IsAllowedAsync(GatewayDispatchContext context, CancellationToken cancellationToken = default);
}

public interface IGatewayCommandHandler
{
    Task HandleAsync(GatewayDispatchContext context, CancellationToken cancellationToken = default);
}

public interface IGatewayRouter
{
    Task<GatewayDispatchResult> DispatchAsync(ChannelMessage message, CancellationToken cancellationToken = default);
}

public sealed class DefaultGatewayRouter(
    IEnumerable<IGatewayPolicy> policies,
    IGatewayCommandHandler commandHandler) : IGatewayRouter
{
    private readonly IReadOnlyList<IGatewayPolicy> _policies = policies.ToList();
    private readonly IGatewayCommandHandler _commandHandler = commandHandler;

    public async Task<GatewayDispatchResult> DispatchAsync(ChannelMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var context = new GatewayDispatchContext(message, DateTimeOffset.UtcNow);

        foreach (var policy in _policies)
        {
            if (!await policy.IsAllowedAsync(context, cancellationToken).ConfigureAwait(false))
            {
                return new GatewayDispatchResult(false, $"Blocked by policy: {policy.Name}");
            }
        }

        await _commandHandler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
        return new GatewayDispatchResult(true);
    }
}

public sealed class AllowAllGatewayPolicy : IGatewayPolicy
{
    public string Name => "allow-all";

    public Task<bool> IsAllowedAsync(GatewayDispatchContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public sealed class GatewayAllowlistOptions
{
    public HashSet<string> Channels { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SenderIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AllowlistGatewayPolicy(IOptions<GatewayAllowlistOptions> options) : IGatewayPolicy
{
    private readonly GatewayAllowlistOptions _options = options.Value;

    public string Name => "allowlist";

    public Task<bool> IsAllowedAsync(GatewayDispatchContext context, CancellationToken cancellationToken = default)
    {
        var channelAllowed = _options.Channels.Count == 0 || _options.Channels.Contains(context.Message.Channel);
        var senderAllowed = _options.SenderIds.Count == 0 || _options.SenderIds.Contains(context.Message.SenderId);
        return Task.FromResult(channelAllowed && senderAllowed);
    }
}

public sealed class NullGatewayCommandHandler : IGatewayCommandHandler
{
    public Task HandleAsync(GatewayDispatchContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAgentsGateway(
        this IServiceCollection services,
        Action<GatewayAllowlistOptions>? configureAllowlist = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<GatewayAllowlistOptions>();
        if (configureAllowlist is not null)
        {
            services.Configure(configureAllowlist);
        }
        services.AddSingleton<IGatewayCommandHandler, NullGatewayCommandHandler>();
        services.AddScoped<IGatewayRouter, DefaultGatewayRouter>();
        services.AddSingleton<IGatewayPolicy, AllowlistGatewayPolicy>();
        return services;
    }
}
