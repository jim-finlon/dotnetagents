using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.PromptRuntime;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PromptRuntime services: registry, client, and a configured IHttpClientFactory named client.
    /// Callers register their prompts with <see cref="IPromptRegistry.Register"/> during startup.
    /// </summary>
    public static IServiceCollection AddDotNetAgentsPromptRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PromptRuntimeOptions>(configuration.GetSection(PromptRuntimeOptions.SectionName));
        services.AddSingleton<IPromptRegistry, PromptRegistry>();
        services.AddSingleton<IPromptRuntimeClient, PromptRuntimeClient>();
        services.AddHttpClient("DotNetAgents.PromptRuntime");
        return services;
    }

    /// <summary>
    /// Convenience for tests / hosts that don't bind Configuration.
    /// </summary>
    public static IServiceCollection AddDotNetAgentsPromptRuntime(
        this IServiceCollection services,
        Action<PromptRuntimeOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IPromptRegistry, PromptRegistry>();
        services.AddSingleton<IPromptRuntimeClient, PromptRuntimeClient>();
        services.AddHttpClient("DotNetAgents.PromptRuntime");
        return services;
    }
}
