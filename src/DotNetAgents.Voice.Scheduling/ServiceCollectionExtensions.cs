using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Voice.Scheduling;

/// <summary>
/// Extension methods for registering scheduled command services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds scheduled command services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScheduledCommands(
        this IServiceCollection services,
        Action<ScheduledCommandOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ScheduledCommandOptions();
        configure?.Invoke(options);

        // Register command store (default to in-memory)
        if (options.CommandStoreFactory != null)
        {
            services.TryAddSingleton<IScheduledCommandStore>(options.CommandStoreFactory);
        }
        else
        {
            services.TryAddSingleton<IScheduledCommandStore, InMemoryScheduledCommandStore>();
        }

        // Register scheduler
        services.TryAddScoped<ICommandScheduler, CommandScheduler>();

        // Register background executor
        if (options.EnableBackgroundExecutor)
        {
            services.AddHostedService<ScheduledCommandExecutor>();
        }

        return services;
    }
}

/// <summary>
/// Options for scheduled command services.
/// </summary>
public class ScheduledCommandOptions
{
    /// <summary>
    /// Gets or sets a factory function for creating the command store.
    /// </summary>
    public Func<IServiceProvider, IScheduledCommandStore>? CommandStoreFactory { get; set; }

    /// <summary>
    /// Gets or sets whether to enable the background executor. Default is true.
    /// </summary>
    public bool EnableBackgroundExecutor { get; set; } = true;
}
