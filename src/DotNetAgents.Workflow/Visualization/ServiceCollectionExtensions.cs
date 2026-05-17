using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Workflow.Visualization;

/// <summary>
/// Extension methods for registering graph visualization services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds graph visualization services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddGraphVisualization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IGraphVisualizationService, GraphVisualizationService>();

        return services;
    }
}
