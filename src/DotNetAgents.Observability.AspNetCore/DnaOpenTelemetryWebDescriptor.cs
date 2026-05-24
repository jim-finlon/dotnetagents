// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Observability.AspNetCore;

/// <summary>
/// Describes DNA-specific OpenTelemetry sources for an ASP.NET Core MCP or API host.
/// </summary>
public sealed class DnaOpenTelemetryWebDescriptor
{
    /// <summary>Stable DNA service key (stored as <c>dna.service</c> resource attribute).</summary>
    public required string DnaServiceKey { get; init; }

    /// <summary>OpenTelemetry <c>service.name</c> when <c>OTEL_SERVICE_NAME</c> / <c>OpenTelemetry:ServiceName</c> are unset.</summary>
    public required string ActivityServiceName { get; init; }

    /// <summary>Additional <see cref="System.Diagnostics.ActivitySource"/> names to subscribe to.</summary>
    public IReadOnlyList<string> ActivitySourceNames { get; init; } = Array.Empty<string>();

    /// <summary>Additional <see cref="System.Diagnostics.Metrics.Meter"/> names to subscribe to.</summary>
    public IReadOnlyList<string> MeterNames { get; init; } = Array.Empty<string>();
}
