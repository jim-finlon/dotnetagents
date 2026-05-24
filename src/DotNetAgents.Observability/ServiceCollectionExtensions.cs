// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Observability.Health;
using DotNetAgents.Observability.Failures;
using DotNetAgents.Observability.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetAgents.Observability;

/// <summary>
/// Extension methods for service collection registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DotNetAgents observability services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDotNetAgentsObservability(
        this IServiceCollection services,
        Action<ObservabilityOptions>? configure = null)
    {
        var options = new ObservabilityOptions();
        configure?.Invoke(options);

        // Register cost tracker
        if (options.CostTracker != null)
        {
            services.AddSingleton(typeof(ICostTracker), options.CostTracker);
        }
        else
        {
            services.AddSingleton<ICostTracker, InMemoryCostTracker>();
        }

        // Register metrics collector
        if (options.MetricsCollector != null)
        {
            services.AddSingleton(typeof(IMetricsCollector), options.MetricsCollector);
        }
        else
        {
            services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();
        }

        // Register agent failure telemetry
        if (options.AgentFailureTelemetryStore != null)
        {
            services.AddSingleton(typeof(IAgentFailureTelemetryStore), options.AgentFailureTelemetryStore);
        }
        else
        {
            services.AddSingleton<IAgentFailureTelemetryStore, InMemoryAgentFailureTelemetryStore>();
        }
        services.AddSingleton(AgentFallbackPolicy.CreateDefault());

        // Register health checks
        if (options.EnableHealthChecks)
        {
            services.AddHealthChecks()
                    .AddCheck<AgentHealthCheck>("dotnetagents", tags: new[] { "dotnetagents" });
        }

        return services;
    }
}

/// <summary>
/// Options for configuring observability.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Gets or sets a custom cost tracker implementation.
    /// </summary>
    public Type? CostTracker { get; set; }

    /// <summary>
    /// Gets or sets a custom metrics collector implementation.
    /// </summary>
    public Type? MetricsCollector { get; set; }

    /// <summary>
    /// Gets or sets a custom agent failure telemetry store implementation.
    /// </summary>
    public Type? AgentFailureTelemetryStore { get; set; }

    /// <summary>
    /// Gets or sets whether to enable health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;
}
