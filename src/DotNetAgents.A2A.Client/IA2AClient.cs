using DotNetAgents.A2A;

namespace DotNetAgents.A2A.Client;

/// <summary>
/// Client SDK for invoking external A2A 1.0 servers from a DNA agent. Discovers Agent Cards,
/// sends tasks, streams events. Wraps HttpClient with SSE parsing.
/// </summary>
public interface IA2AClient
{
    /// <summary>
    /// Discover an Agent Card at the given base URL. Hits <c>{baseUrl}/.well-known/agent.json</c>;
    /// caches the result for the configured TTL (default 5min) so repeat callers don't re-fetch.
    /// </summary>
    Task<AgentCard> DiscoverAsync(Uri baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an A2A task synchronously and await the response. Use for short-lived tasks where
    /// streaming is unnecessary.
    /// </summary>
    Task<A2AResponse> SendTaskAsync(
        Uri baseUrl,
        A2ATask task,
        A2AClientCallOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an A2A task and stream events as they arrive (SSE). Use for long-running tasks
    /// where intermediate progress matters.
    /// </summary>
    /// <remarks>
    /// SSE reconnect on dropped connection is the responsibility of the caller for now —
    /// a follow-up story will add Last-Event-Id resume per the A2A spec.
    /// </remarks>
    IAsyncEnumerable<A2AEvent> StreamTaskAsync(
        Uri baseUrl,
        A2ATask task,
        A2AClientCallOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Per-call options for an A2A invocation. Configures auth, timeouts, base-route override.
/// </summary>
public sealed record A2AClientCallOptions
{
    /// <summary>Optional bearer token for the Authorization header. Resolve from CredentialsAgent at call site.</summary>
    public string? BearerToken { get; init; }

    /// <summary>Override the server's base route (default <c>/a2a/v1</c>) when the target uses a non-standard path.</summary>
    public string BaseRoute { get; init; } = "/a2a/v1";

    /// <summary>Per-call timeout. Default 30s for synchronous send; ignored for streaming (use cancellation).</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Skip Agent Card validation against pinned trust list. Test/demo-only.</summary>
    public bool SkipAgentCardValidation { get; init; } = false;
}
