// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ModelRouting;

/// <summary>Request context for model routing. FR-MR-001, FR-MR-002.</summary>
public sealed record RoutingRequest
{
    /// <summary>Optional input preview (e.g. first N chars) for capability or size hints.</summary>
    public string? InputPreview { get; init; }

    /// <summary>Optional required capabilities (e.g. "vision", "function_calling", "long_context").</summary>
    public IReadOnlySet<string>? RequiredCapabilities { get; init; }

    /// <summary>Optional metadata for custom routing logic.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
