using DotNetAgents.Security.Audit;
using DotNetAgents.Security.RateLimiting;
using DotNetAgents.Security.Secrets;
using DotNetAgents.Security.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Security;

/// <summary>
/// Extension methods for service collection registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DotNetAgents security services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDotNetAgentsSecurity(
        this IServiceCollection services,
        Action<SecurityOptions>? configure = null)
    {
        var options = new SecurityOptions();
        configure?.Invoke(options);

        // Register secrets provider
        if (options.SecretsProvider != null)
        {
            services.AddSingleton(typeof(ISecretsProvider), options.SecretsProvider);
        }
        else
        {
            services.AddSingleton<ISecretsProvider, EnvironmentSecretsProvider>();
        }

        // Register sanitizer
        if (options.Sanitizer != null)
        {
            services.AddSingleton(typeof(ISanitizer), options.Sanitizer);
        }
        else
        {
            services.AddSingleton<ISanitizer, BasicSanitizer>();
        }

        // Register rate limiter
        if (options.RateLimiter != null)
        {
            services.AddSingleton(typeof(IRateLimiter), options.RateLimiter);
        }
        else
        {
            services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        }

        // Register audit logger
        if (options.AuditLogger != null)
        {
            services.AddSingleton(typeof(IAuditLogger), options.AuditLogger);
        }
        else
        {
            services.AddSingleton<IAuditLogger, ConsoleAuditLogger>();
        }

        return services;
    }
}

/// <summary>
/// Options for configuring security services.
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// Gets or sets a custom secrets provider implementation.
    /// </summary>
    public Type? SecretsProvider { get; set; }

    /// <summary>
    /// Gets or sets a custom sanitizer implementation.
    /// </summary>
    public Type? Sanitizer { get; set; }

    /// <summary>
    /// Gets or sets a custom rate limiter implementation.
    /// </summary>
    public Type? RateLimiter { get; set; }

    /// <summary>
    /// Gets or sets a custom audit logger implementation.
    /// </summary>
    public Type? AuditLogger { get; set; }
}
