using System.ComponentModel.DataAnnotations;

namespace DotNetAgents.Hosting;

/// <summary>
/// A2A host-profile options bound by <see cref="DnaA2AHostExtensions.AddDnaA2AHost"/>.
/// Services still own their concrete agents and authorization policy.
/// </summary>
public sealed class DnaA2AHostOptions
{
    /// <summary>Configuration section path used by AddDnaA2AHost when binding.</summary>
    public const string SectionPath = "DotNetAgents:Hosting:A2A";

    /// <summary>Service-level Agent Card display name. Required.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Service-level Agent Card description.</summary>
    public string ServiceDescription { get; set; } = "DNA-hosted A2A agent surface.";

    /// <summary>Service-level Agent Card version.</summary>
    public string ServiceVersion { get; set; } = "1.0";

    /// <summary>Agent card discovery path. Default <c>/.well-known/agent.json</c>.</summary>
    public string AgentCardPath { get; set; } = "/.well-known/agent.json";

    /// <summary>Base path for task endpoints. Default <c>/a2a/v1</c>.</summary>
    public string BaseRoute { get; set; } = "/a2a/v1";

    /// <summary>
    /// Whether this host requires signed identity-card posture. The service-owned
    /// <c>IA2ARequestAuthorizer</c> enforces the concrete trust policy.
    /// </summary>
    public bool RequireSignedIdentityCard { get; set; } = true;

    /// <summary>Optional public base URL advertised in operator docs and deployment receipts.</summary>
    public string AdvertisedBaseUrl { get; set; } = string.Empty;

    /// <summary>Expected skill ids advertised by this host. Concrete registered agents remain authoritative.</summary>
    public IList<string> Skills { get; set; } = new List<string>();

    /// <summary>A2A auth-mode configuration section. Default <c>DotNetAgents:A2A:Server:Auth</c>.</summary>
    public string AuthModeSection { get; set; } = "DotNetAgents:A2A:Server:Auth";

    /// <summary>When true, requests without a bearer token are rejected by the A2A transport layer.</summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>When true, non-loopback callers may reach the service-owned A2A authorizer.</summary>
    public bool AllowNonLoopbackRequests { get; set; }

    /// <summary>Maximum wall-clock duration for one A2A task.</summary>
    public TimeSpan MaxTaskDuration { get; set; } = TimeSpan.FromMinutes(10);
}
