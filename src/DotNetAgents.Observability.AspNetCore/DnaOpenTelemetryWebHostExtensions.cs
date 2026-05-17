using DotNetAgents.Observability.Otel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DotNetAgents.Observability.AspNetCore;

/// <summary>
/// Registers OpenTelemetry traces and metrics for DNA ASP.NET Core hosts, including optional OTLP export from <c>OTEL_*</c> variables.
/// </summary>
public static class DnaOpenTelemetryWebHostExtensions
{
    /// <summary>
    /// Adds OpenTelemetry with ASP.NET Core + HTTP client instrumentation, DNA resource attributes, and optional OTLP exporters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// OTLP is enabled when <c>OTEL_EXPORTER_OTLP_TRACES_ENDPOINT</c> / <c>OTEL_EXPORTER_OTLP_METRICS_ENDPOINT</c> or
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> resolve to an absolute URI (see <see cref="DnaOtlpConfiguration"/>).
    /// </para>
    /// <para>
    /// When no OTLP endpoints are set and the host environment is <see cref="Environments.Development"/>, a console exporter is
    /// attached so local dev keeps visible spans without a collector.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddDnaOpenTelemetryForWebHost(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        DnaOpenTelemetryWebDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(descriptor);

        var enabled = configuration.GetValue("OpenTelemetry:Enabled", true);
        if (!enabled)
        {
            return services;
        }

        var otlp = DnaOtlpConfiguration.Resolve(configuration);
        var serviceName = configuration["OTEL_SERVICE_NAME"]
            ?? configuration["OpenTelemetry:ServiceName"]
            ?? descriptor.ActivityServiceName;
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var serviceNamespace = configuration["OpenTelemetry:ServiceNamespace"] ?? "dna";
        var deploymentEnvironment = hostEnvironment.EnvironmentName;

        var extraResource = DnaOtlpConfiguration.ParseResourceAttributes(configuration["OTEL_RESOURCE_ATTRIBUTES"]);

        var useConsoleFallback = otlp.TraceUri is null
            && otlp.MetricUri is null
            && hostEnvironment.IsDevelopment();

        var resourceAttrs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["service.namespace"] = serviceNamespace,
            ["dna.service"] = descriptor.DnaServiceKey,
            ["dna.platform"] = "dotnetagents",
            ["deployment.environment"] = deploymentEnvironment,
        };
        foreach (var kv in extraResource)
        {
            resourceAttrs[kv.Key] = kv.Value;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(resourceAttrs))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                foreach (var name in descriptor.ActivitySourceNames)
                {
                    tracing.AddSource(name);
                }

                if (otlp.TraceUri is not null)
                {
                    tracing.AddOtlpExporter(o => DnaOtlpConfiguration.Apply(o, otlp, otlp.TraceUri));
                }
                else if (useConsoleFallback)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                foreach (var m in descriptor.MeterNames)
                {
                    metrics.AddMeter(m);
                }

                if (otlp.MetricUri is not null)
                {
                    metrics.AddOtlpExporter(o => DnaOtlpConfiguration.Apply(o, otlp, otlp.MetricUri));
                }
                else if (useConsoleFallback)
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }
}
