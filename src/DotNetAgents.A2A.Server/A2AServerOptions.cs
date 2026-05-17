namespace DotNetAgents.A2A.Server;

/// <summary>
/// Configuration for the <see cref="A2AServerEndpoints"/> mapping. Bind from the host's
/// <c>"A2A:Server"</c> config section or configure inline at registration time.
/// </summary>
public sealed class A2AServerOptions
{
    /// <summary>
    /// Display name for the hosted service-level AgentCard. When multiple agents are registered,
    /// this becomes the aggregate card name returned from <c>/.well-known/agent.json</c>.
    /// </summary>
    public string ServiceName { get; set; } = "DNA A2A Service";

    /// <summary>
    /// Description for the hosted service-level AgentCard.
    /// </summary>
    public string ServiceDescription { get; set; } = "DNA-hosted A2A agent surface.";

    /// <summary>
    /// Version string for the hosted service-level AgentCard.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0";

    /// <summary>Agent card discovery path. Default <c>/.well-known/agent.json</c>.</summary>
    public string AgentCardPath { get; set; } = "/.well-known/agent.json";

    /// <summary>Base path for A2A endpoints. Default <c>/a2a/v1</c>.</summary>
    public string BaseRoute { get; set; } = "/a2a/v1";

    /// <summary>
    /// Operator-curated bearer token allowlist. Empty list means no auth (test/dev use). Production
    /// deployments populate this from CredentialsAgent at startup. Tokens are compared
    /// case-sensitively against the inbound <c>Authorization: Bearer &lt;token&gt;</c> header.
    /// </summary>
    public IList<string> AllowedBearerTokens { get; set; } = new List<string>();

    /// <summary>
    /// When true, requests without a bearer token are rejected with 401. When false (default),
    /// missing tokens are allowed if <see cref="AllowedBearerTokens"/> is empty.
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

    /// <summary>
    /// SSE keep-alive interval for streaming responses. Default 15s — long enough that low-volume
    /// streams don't spam, short enough to detect dropped connections within a reasonable window.
    /// </summary>
    public TimeSpan SseKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Maximum wall-clock duration for a single task (sync or stream). Default 10 minutes.
    /// </summary>
    public TimeSpan MaxTaskDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// When false (default), non-loopback callers are rejected before task execution. This keeps
    /// new A2A surfaces local-only until a stronger trust-binding pipeline is enabled.
    /// </summary>
    public bool AllowNonLoopbackRequests { get; set; } = false;
}
