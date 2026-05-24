// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Configuration;

/// <summary>
/// Extension methods for registering DotNetAgents services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DotNetAgents services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure DotNetAgents.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDotNetAgents(
        this IServiceCollection services,
        Action<ConfigurationBuilder>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var builder = new ConfigurationBuilder();
        configure?.Invoke(builder);

        var configuration = builder.BuildWithoutValidation();

        services.AddSingleton(Options.Create(configuration));
        services.AddSingleton<AgentConfiguration>(configuration);

        return services;
    }

    /// <summary>
    /// Adds DotNetAgents services from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration source.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDotNetAgents(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var builder = new ConfigurationBuilder();
        builder.FromConfiguration(configuration);

        var agentConfiguration = builder.BuildWithoutValidation();

        services.AddSingleton(Options.Create(agentConfiguration));
        services.AddSingleton<AgentConfiguration>(agentConfiguration);

        return services;
    }
}
