using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.AgentFramework;

/// <summary>
/// Extension methods for registering DotNetAgents components with Microsoft Agent Framework.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DotNetAgents integration with Microsoft Agent Framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method will register adapters and integration components when Microsoft Agent Framework APIs stabilize.
    /// Currently, it sets up the foundation for future integration.
    /// </remarks>
    public static IServiceCollection AddDotNetAgentsAgentFrameworkIntegration(
        this IServiceCollection services,
        Action<AgentFrameworkIntegrationOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var options = new AgentFrameworkIntegrationOptions();
        configure?.Invoke(options);

        // Register adapter interfaces (implementations will be added when MAF APIs stabilize)
        // services.AddSingleton<IToolAdapter, ToolAdapter>();
        // services.AddSingleton<IChainAdapter, ChainAdapter>();
        // services.AddSingleton<IDocumentLoaderTool, DocumentLoaderToolAdapter>();

        return services;
    }
}

/// <summary>
/// Configuration options for DotNetAgents and Microsoft Agent Framework integration.
/// </summary>
public class AgentFrameworkIntegrationOptions
{
    /// <summary>
    /// Gets or sets whether to expose document loaders as MAF tools.
    /// </summary>
    public bool ExposeDocumentLoaders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to expose built-in DotNetAgents tools as MAF tools.
    /// </summary>
    public bool ExposeBuiltInTools { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register DotNetAgents LLM providers with MAF.
    /// </summary>
    public bool RegisterLLMProviders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable chain-to-workflow conversion.
    /// </summary>
    public bool EnableChainConversion { get; set; } = true;
}
