using System.ComponentModel.DataAnnotations;

namespace DotNetAgents.Hosting;

/// <summary>
/// Baseline service host options bound by <see cref="DnaServiceHostExtensions.AddDnaServiceHost"/>.
/// Owned by service-specific composition; the hosting package itself reads these as a snapshot.
/// </summary>
public sealed class DnaServiceHostOptions
{
    /// <summary>Configuration section path used by AddDnaServiceHost when binding.</summary>
    public const string SectionPath = "DotNetAgents:Hosting";

    /// <summary>Short stable name (e.g. "sdlc-agent", "credentials-agent"). Required.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Human-readable display name. Defaults to <see cref="ServiceName"/> when empty.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Informational version string. Optional.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Deployment ring (e.g. "dev", "canary", "prime"). Optional.</summary>
    public string DeploymentRing { get; set; } = string.Empty;

    /// <summary>Enable ProblemDetails defaults via Microsoft.AspNetCore.Http.AddProblemDetails.</summary>
    public bool EnableProblemDetails { get; set; } = true;

    /// <summary>Record a <see cref="DnaStartupReceipt"/> on host startup.</summary>
    public bool EnableStartupReceipt { get; set; } = true;

    /// <summary>Live probe path. Default <c>/health/live</c>.</summary>
    public string HealthLivePath { get; set; } = "/health/live";

    /// <summary>Ready probe path. Default <c>/health/ready</c>.</summary>
    public string HealthReadyPath { get; set; } = "/health/ready";

    /// <summary>Aggregate health path. Default <c>/health</c>.</summary>
    public string HealthAggregatePath { get; set; } = "/health";

    /// <summary>Optional operator runbook URL surfaced in startup receipts.</summary>
    public string OperatorRunbookPath { get; set; } = string.Empty;
}
