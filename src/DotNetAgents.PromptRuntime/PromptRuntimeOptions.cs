// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.PromptRuntime;

/// <summary>
/// Host-side configuration for the PromptSpecialist runtime client. Bound from the
/// <c>DotNetAgents:PromptRuntime</c> section of IConfiguration.
/// </summary>
public sealed class PromptRuntimeOptions
{
    public const string SectionName = "DotNetAgents:PromptRuntime";

    /// <summary>PromptSpecialist base URL (e.g. http://prompt-specialist-agent:5090). Empty = always fall back.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Optional X-Api-Key header value.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Round-trip timeout. Defaults to 3s so a slow library doesn't stall callers.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>How long in-process cache entries stay warm. 0 disables caching.</summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>When true, never reach out to PromptSpecialist — always serve from registry. Useful in tests.</summary>
    public bool LocalOnly { get; set; }

    /// <summary>When true, outcome reports are disabled.</summary>
    public bool DisableOutcomeReports { get; set; }
}
