using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DotNetAgents.Contracts;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="GeneticContractReader"/> as a singleton bound to the host's
    /// <see cref="IHostEnvironment.ContentRootPath"/>. <paramref name="serviceLabel"/> is used
    /// only in diagnostic messages when the contract cannot be located.
    /// </summary>
    public static IServiceCollection AddGeneticContractReader(
        this IServiceCollection services,
        string serviceLabel)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceLabel);

        services.TryAddSingleton(sp =>
            new GeneticContractReader(sp.GetRequiredService<IHostEnvironment>(), serviceLabel));
        return services;
    }
}
