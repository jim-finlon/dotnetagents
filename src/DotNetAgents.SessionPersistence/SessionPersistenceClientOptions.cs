// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence;

/// <summary>
/// Options for the AI Session Persistence HTTP client.
/// </summary>
public class SessionPersistenceClientOptions
{
    public const string SectionName = "SessionPersistence";

    /// <summary>Base URL of the AI Session Persistence API (e.g. http://localhost:5000).</summary>
    public string BaseAddress { get; set; } = "http://localhost:5000";

    /// <summary>Optional API key for X-API-Key header.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Timeout for HTTP requests (default 30s).</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
