using DotNetAgents.Abstractions.PublicSubstitutes.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Core.PublicSubstitutes.Identity;

/// <summary>DI helpers for the configuration-backed public agent identity adapter.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="ConfigurationAgentIdentityDescriptor"/> as the active
    /// <see cref="IAgentIdentityDescriptor"/>. Hosts should register
    /// <c>IConfiguration</c> when they want to override the safe local defaults.
    /// </summary>
    public static IServiceCollection AddConfigurationAgentIdentityDescriptor(this IServiceCollection services)
    {
        services.TryAddSingleton<IAgentIdentityDescriptor, ConfigurationAgentIdentityDescriptor>();
        return services;
    }
}
