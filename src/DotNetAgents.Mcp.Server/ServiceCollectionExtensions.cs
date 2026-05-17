using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Server.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Mcp.Server;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpLifecycleHooks(this IServiceCollection services)
    {
        services.TryAddSingleton<IMcpEventSink>(_ => NoOpMcpEventSink.Instance);
        services.TryAddSingleton<IMcpSafetyVerifier>(_ => NoOpMcpSafetyVerifier.Instance);

        // When the consumer has also registered an IAgentLearningProjector (typical for DNA services
        // that call AddDnaAgentLearningProjection()), prefer a publisher that bridges every MCP tool-call
        // emission into that projection pipeline. Otherwise fall back to the no-op publisher so the
        // contract stays non-breaking.
        services.TryAddSingleton<ILessonEventPublisher>(sp =>
        {
            var projector = sp.GetService<IAgentLearningProjector>();
            if (projector is null)
            {
                return NoOpLessonEventPublisher.Instance;
            }

            var logger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger<ProjectingLessonEventPublisher>();
            return new ProjectingLessonEventPublisher(projector, logger);
        });

        return services;
    }

    public static IServiceCollection AddUnifiedToolRegistry(this IServiceCollection services)
    {
        services.TryAddSingleton<IUnifiedToolRegistry, InMemoryUnifiedToolRegistry>();
        return services;
    }
}
