// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.A2A;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.A2A.Server;

/// <summary>
/// DI registration helpers for the A2A server. Story c46e33de.
/// </summary>
public static class A2AServerServiceCollectionExtensions
{
    /// <summary>
    /// Register the A2A server middleware + a default in-memory agent registry. Operators
    /// configure <see cref="A2AServerOptions"/> via <paramref name="configure"/> or the
    /// <c>A2A:Server</c> config section.
    /// </summary>
    public static IServiceCollection AddA2AServer(
        this IServiceCollection services,
        Action<A2AServerOptions>? configure = null)
    {
        services.TryAddSingleton<IA2AAgentRegistry, InMemoryA2AAgentRegistry>();
        services.TryAddSingleton<IA2ARequestAuthorizer, LoopbackOnlyA2ARequestAuthorizer>();
        services.TryAddSingleton<IA2ASkillPolicy, AllowAllA2ASkillPolicy>();

        var optionsBuilder = services.AddOptions<A2AServerOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        return services;
    }

    /// <summary>
    /// Bind <see cref="A2AServerOptions"/> from a config section (typically <c>"A2A:Server"</c>).
    /// </summary>
    public static IServiceCollection AddA2AServer(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(configurationSection);
        services.TryAddSingleton<IA2AAgentRegistry, InMemoryA2AAgentRegistry>();
        services.TryAddSingleton<IA2ARequestAuthorizer, LoopbackOnlyA2ARequestAuthorizer>();
        services.TryAddSingleton<IA2ASkillPolicy, AllowAllA2ASkillPolicy>();
        services.AddOptions<A2AServerOptions>().Bind(configurationSection);
        return services;
    }

    /// <summary>
    /// Register a single agent into the registry at startup. Convenience for hosts that publish
    /// exactly one A2A agent.
    /// </summary>
    public static IServiceCollection AddA2AAgent(
        this IServiceCollection services,
        string id,
        Func<IServiceProvider, IA2AAgent> factory)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(factory);

        services.AddSingleton(sp =>
        {
            var registry = sp.GetRequiredService<IA2AAgentRegistry>();
            var agent = factory(sp);
            registry.Register(id, agent);
            return new RegistrationMarker(id);
        });
        return services;
    }

    /// <summary>Marker type emitted by <see cref="AddA2AAgent"/> so the registration is forced to resolve at app start.</summary>
    public sealed record RegistrationMarker(string Id);
}
