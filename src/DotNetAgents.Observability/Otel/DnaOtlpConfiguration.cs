using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;

namespace DotNetAgents.Observability.Otel;

/// <summary>
/// Resolves standard <c>OTEL_*</c> exporter settings from <see cref="IConfiguration"/> (includes environment
/// variables when the default host builder merges them). Used by DNA web hosts and samples for OTLP.
/// </summary>
public static class DnaOtlpConfiguration
{
    /// <summary>
    /// Optional OTLP endpoints and protocol hints derived from configuration / environment.
    /// </summary>
    public sealed record DnaOtlpEndpoints(
        Uri? TraceUri,
        Uri? MetricUri,
        string? ExplicitProtocol,
        string? Headers)
    {
        /// <summary>
        /// Chooses <see cref="OtlpExportProtocol"/> for a concrete <paramref name="endpoint"/> using
        /// <c>OTEL_EXPORTER_OTLP_PROTOCOL</c> when set; otherwise port <c>4318</c> implies HTTP/protobuf.
        /// </summary>
        public OtlpExportProtocol ProtocolFor(Uri endpoint) => ParseProtocolForEndpoint(ExplicitProtocol, endpoint);
    }

    /// <summary>
    /// Resolves OTLP endpoints using the OpenTelemetry environment variable precedence used across DNA runbooks.
    /// </summary>
    /// <remarks>
    /// Trace: <c>OTEL_EXPORTER_OTLP_TRACES_ENDPOINT</c> then <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>.
    /// Metrics: <c>OTEL_EXPORTER_OTLP_METRICS_ENDPOINT</c> then <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>.
    /// Protocol: <c>OTEL_EXPORTER_OTLP_PROTOCOL</c> (<c>grpc</c> | <c>http/protobuf</c>); when unset, port <c>4318</c> implies HTTP/protobuf, other ports default to gRPC.
    /// Headers: <c>OTEL_EXPORTER_OTLP_HEADERS</c> as <c>key=value,key2=value2</c> (passed through to the exporter).
    /// </remarks>
    public static DnaOtlpEndpoints Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var traceRaw = FirstNonEmpty(
            configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"],
            configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        var metricRaw = FirstNonEmpty(
            configuration["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"],
            configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        Uri? traceUri = TryParseUri(traceRaw);
        Uri? metricUri = TryParseUri(metricRaw);

        var headers = configuration["OTEL_EXPORTER_OTLP_HEADERS"];
        var explicitProtocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];
        return new DnaOtlpEndpoints(traceUri, metricUri, explicitProtocol, headers);
    }

    /// <summary>
    /// Applies resolved endpoints to <see cref="OtlpExporterOptions"/> (endpoint, protocol, headers string).
    /// </summary>
    public static void Apply(OtlpExporterOptions options, DnaOtlpEndpoints endpoints, Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(endpoint);
        options.Endpoint = endpoint;
        options.Protocol = endpoints.ProtocolFor(endpoint);
        if (!string.IsNullOrWhiteSpace(endpoints.Headers))
        {
            options.Headers = endpoints.Headers;
        }
    }

    /// <summary>
    /// Parses <c>OTEL_RESOURCE_ATTRIBUTES</c> (<c>key=value,key2=value2</c>) into discrete entries suitable for <c>ResourceBuilder.AddAttributes</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, object> ParseResourceAttributes(string? otelResourceAttributes)
    {
        if (string.IsNullOrWhiteSpace(otelResourceAttributes))
        {
            return new Dictionary<string, object>();
        }

        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in otelResourceAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0 || eq >= part.Length - 1)
            {
                continue;
            }

            var key = part[..eq].Trim();
            var value = part[(eq + 1)..].Trim();
            if (key.Length > 0)
            {
                dict[key] = value;
            }
        }

        return dict;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return null;
    }

    private static Uri? TryParseUri(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri) ? uri : null;
    }

    private static OtlpExportProtocol ParseProtocolForEndpoint(string? explicitValue, Uri endpoint)
    {
        if (!string.IsNullOrWhiteSpace(explicitValue))
        {
            if (explicitValue.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase))
            {
                return OtlpExportProtocol.HttpProtobuf;
            }

            if (explicitValue.Equals("grpc", StringComparison.OrdinalIgnoreCase))
            {
                return OtlpExportProtocol.Grpc;
            }
        }

        return endpoint.Port == 4318
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
    }
}
