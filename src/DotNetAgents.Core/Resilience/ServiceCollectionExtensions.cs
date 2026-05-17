using DotNetAgents.Abstractions.Models;
using DotNetAgents.Abstractions.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Core.Resilience;

/// <summary>
/// Extension methods for registering resilient LLM models with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Wraps an LLM model with retry logic and circuit breaker protection.
    /// </summary>
    /// <typeparam name="TInput">The type of input expected by the model.</typeparam>
    /// <typeparam name="TOutput">The type of output produced by the model.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="retryPolicyOptions">Optional retry policy configuration.</param>
    /// <param name="circuitBreakerOptions">Optional circuit breaker configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddResilientLLMModel<TInput, TOutput>(
        this IServiceCollection services,
        RetryPolicyOptions? retryPolicyOptions = null,
        CircuitBreakerOptions? circuitBreakerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register retry policy if options provided
        if (retryPolicyOptions != null)
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetService<ILogger<RetryPolicy>>();
                return new RetryPolicy(retryPolicyOptions, logger);
            });
        }

        // Register circuit breaker if options provided
        if (circuitBreakerOptions != null)
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetService<ILogger<CircuitBreaker>>();
                return new CircuitBreaker(circuitBreakerOptions, logger);
            });
        }

        // Register resilient wrapper factory
        services.AddTransient<Func<ILLMModel<TInput, TOutput>, ResilientLLMModel<TInput, TOutput>>>(sp =>
        {
            var retryPolicy = sp.GetService<RetryPolicy>();
            var circuitBreaker = sp.GetService<CircuitBreaker>();
            var logger = sp.GetService<ILogger<ResilientLLMModel<TInput, TOutput>>>();
            return model => new ResilientLLMModel<TInput, TOutput>(model, retryPolicy, circuitBreaker, logger);
        });

        return services;
    }
}
