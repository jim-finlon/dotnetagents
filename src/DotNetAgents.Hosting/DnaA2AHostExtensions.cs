using DotNetAgents.A2A.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Hosting;

/// <summary>
/// A2A-specific host-profile helpers for DNA ASP.NET Core services.
/// </summary>
public static class DnaA2AHostExtensions
{
    /// <summary>
    /// Registers the shared A2A host profile while leaving concrete agents, request
    /// authorization, and skill policy to the service.
    /// </summary>
    public static IServiceCollection AddDnaA2AHost(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DnaA2AHostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(DnaA2AHostOptions.SectionPath);
        services.AddOptions<DnaA2AHostOptions>()
            .Bind(section)
            .PostConfigure(options =>
            {
                configure?.Invoke(options);
                ApplyDefaults(options);
            })
            .ValidateDataAnnotations();

        var snapshot = ResolveSnapshot(section, configure);
        services.AddA2AServer(options =>
        {
            options.ServiceName = snapshot.ServiceName;
            options.ServiceDescription = snapshot.ServiceDescription;
            options.ServiceVersion = snapshot.ServiceVersion;
            options.AgentCardPath = NormalizePath(snapshot.AgentCardPath, "/.well-known/agent.json");
            options.BaseRoute = NormalizePath(snapshot.BaseRoute, "/a2a/v1");
            options.RequireAuthentication = snapshot.RequireAuthentication;
            options.AllowNonLoopbackRequests = snapshot.AllowNonLoopbackRequests;
            options.MaxTaskDuration = snapshot.MaxTaskDuration;
        });

        return services;
    }

    /// <summary>
    /// Resolves all registered A2A agent markers and maps the configured Agent Card plus task endpoints.
    /// </summary>
    public static IEndpointConventionBuilder MapDnaA2AHost(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints.ServiceProvider.GetRequiredService<IOptions<DnaA2AHostOptions>>().Value;
        foreach (var marker in endpoints.ServiceProvider.GetServices<A2AServerServiceCollectionExtensions.RegistrationMarker>())
        {
            _ = marker;
        }

        return endpoints.MapA2AServer();
    }

    private static DnaA2AHostOptions ResolveSnapshot(
        IConfigurationSection section,
        Action<DnaA2AHostOptions>? configure)
    {
        var options = new DnaA2AHostOptions();
        section.Bind(options);
        configure?.Invoke(options);
        ApplyDefaults(options);
        return options;
    }

    private static void ApplyDefaults(DnaA2AHostOptions options)
    {
        options.ServiceDescription = string.IsNullOrWhiteSpace(options.ServiceDescription)
            ? "DNA-hosted A2A agent surface."
            : options.ServiceDescription;
        options.ServiceVersion = string.IsNullOrWhiteSpace(options.ServiceVersion)
            ? "1.0"
            : options.ServiceVersion;
        options.AgentCardPath = NormalizePath(options.AgentCardPath, "/.well-known/agent.json");
        options.BaseRoute = NormalizePath(options.BaseRoute, "/a2a/v1");
        options.AuthModeSection = string.IsNullOrWhiteSpace(options.AuthModeSection)
            ? "DotNetAgents:A2A:Server:Auth"
            : options.AuthModeSection;
        options.Skills ??= new List<string>();
    }

    private static string NormalizePath(string configured, string fallback)
        => string.IsNullOrWhiteSpace(configured) ? fallback : configured;
}
