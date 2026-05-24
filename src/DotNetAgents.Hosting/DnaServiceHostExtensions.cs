using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetAgents.Hosting;

/// <summary>
/// Composition entry points for the DNA service host. Each method is additive; service code
/// chooses which profile helpers (health, ProblemDetails, MCP, A2A) to opt into.
/// </summary>
public static class DnaServiceHostExtensions
{
    /// <summary>
    /// Registers baseline service host services: <see cref="DnaServiceHostOptions"/> binding,
    /// optional ProblemDetails defaults, and the startup receipt store. Idempotent for the
    /// receipt store; option configurations stack.
    /// </summary>
    public static IServiceCollection AddDnaServiceHost(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        Action<DnaServiceHostOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var section = configuration.GetSection(DnaServiceHostOptions.SectionPath);

        var optionsBuilder = services.AddOptions<DnaServiceHostOptions>()
            .Bind(section);

        if (configure is not null)
            optionsBuilder.Configure(configure);

        // Environment defaults fill any blanks the user/config left, so they must run AFTER
        // the user's configure delegate. PostConfigure runs after the Configure chain.
        optionsBuilder
            .PostConfigure<IHostEnvironment>(ApplyEnvironmentDefaults)
            .ValidateDataAnnotations();

        // Resolve a synchronous snapshot so registration-time decisions (AddProblemDetails)
        // honour the same configure pipeline runtime IOptions resolution will see.
        var snapshot = ResolveSnapshot(section, environment, configure);

        if (!services.Any(d => d.ServiceType == typeof(IDnaStartupReceiptStore)))
            services.AddSingleton<IDnaStartupReceiptStore, DnaStartupReceiptStore>();

        if (snapshot.EnableProblemDetails)
            services.AddProblemDetails();

        if (snapshot.EnableStartupReceipt)
            services.AddHostedService<DnaStartupReceiptHostedService>();

        return services;
    }

    /// <summary>
    /// Maps the three canonical DNA health endpoints (<c>/health</c>, <c>/health/live</c>,
    /// <c>/health/ready</c>) using the paths configured on <see cref="DnaServiceHostOptions"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapDnaHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<DnaServiceHostOptions>>()
            .Value;

        endpoints.MapHealthChecks(NormalizePath(options.HealthAggregatePath, "/health"));
        endpoints.MapHealthChecks(NormalizePath(options.HealthReadyPath, "/health/ready"));
        endpoints.MapHealthChecks(NormalizePath(options.HealthLivePath, "/health/live"));

        return endpoints;
    }

    /// <summary>
    /// Returns a snapshot of the resolved host options as they would appear at runtime — the
    /// same Bind + environment-default + configure pipeline used by IOptions resolution. Useful
    /// for registration-time decisions in this method without forcing callers to build a
    /// throwaway provider.
    /// </summary>
    private static DnaServiceHostOptions ResolveSnapshot(
        IConfigurationSection section,
        IHostEnvironment environment,
        Action<DnaServiceHostOptions>? configure)
    {
        var options = new DnaServiceHostOptions();
        section.Bind(options);
        configure?.Invoke(options);
        ApplyEnvironmentDefaults(options, environment);
        return options;
    }

    private static void ApplyEnvironmentDefaults(DnaServiceHostOptions options, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(options.DisplayName))
            options.DisplayName = options.ServiceName;

        if (string.IsNullOrWhiteSpace(options.DeploymentRing))
            options.DeploymentRing = environment.EnvironmentName;
    }

    private static string NormalizePath(string configured, string fallback)
        => string.IsNullOrWhiteSpace(configured) ? fallback : configured;
}

internal sealed class DnaStartupReceiptHostedService(
    IOptions<DnaServiceHostOptions> options,
    IDnaStartupReceiptStore store,
    IHostEnvironment environment,
    ILogger<DnaStartupReceiptHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var snapshot = options.Value;
        if (!snapshot.EnableStartupReceipt)
            return Task.CompletedTask;

        var receipt = new DnaStartupReceipt(
            ServiceName: snapshot.ServiceName,
            DisplayName: string.IsNullOrWhiteSpace(snapshot.DisplayName) ? snapshot.ServiceName : snapshot.DisplayName,
            Version: snapshot.Version,
            DeploymentRing: string.IsNullOrWhiteSpace(snapshot.DeploymentRing) ? environment.EnvironmentName : snapshot.DeploymentRing,
            StartedAtUtc: DateTimeOffset.UtcNow,
            EnvironmentName: environment.EnvironmentName,
            ProblemDetailsEnabled: snapshot.EnableProblemDetails,
            HealthLivePath: snapshot.HealthLivePath,
            HealthReadyPath: snapshot.HealthReadyPath,
            HealthAggregatePath: snapshot.HealthAggregatePath,
            OperatorRunbookPath: snapshot.OperatorRunbookPath);

        store.Record(receipt);

        logger.LogInformation(
            "dna_service_host_started service={ServiceName} version={Version} ring={DeploymentRing} env={EnvironmentName}",
            receipt.ServiceName,
            string.IsNullOrWhiteSpace(receipt.Version) ? "unknown" : receipt.Version,
            receipt.DeploymentRing,
            receipt.EnvironmentName);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
