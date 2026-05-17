using DotNetAgents.Abstractions.PublicSubstitutes.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Tasks.PublicSubstitutes.Tasks;

/// <summary>Registration helpers for public task/work-state substitutes.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryTaskStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IPublicTaskStore, InMemoryTaskStore>();
        return services;
    }

    public static IServiceCollection AddFileBackedTaskStore(
        this IServiceCollection services,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IPublicTaskStore>(_ => new FileBackedTaskStore(filePath));
        return services;
    }
}
