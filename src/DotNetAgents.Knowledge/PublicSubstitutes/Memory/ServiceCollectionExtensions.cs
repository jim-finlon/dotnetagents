using DotNetAgents.Abstractions.PublicSubstitutes.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Knowledge.PublicSubstitutes.Memory;

/// <summary>Registration helpers for public durable-memory substitutes.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryPublicLessonStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IPublicLessonStore, InMemoryPublicLessonStore>();
        return services;
    }
}
