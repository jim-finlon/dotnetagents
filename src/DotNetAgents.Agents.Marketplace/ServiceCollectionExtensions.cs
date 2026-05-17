using DotNetAgents.Ecosystem;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Agents.Marketplace;

/// <summary>
/// Extension methods for registering agent marketplace services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds agent marketplace support to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentMarketplace(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the marketplace plugin
        services.AddPlugin(new MarketplacePlugin());

        return services;
    }
}
