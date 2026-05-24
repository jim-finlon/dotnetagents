// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Agents;

/// <summary>
/// Correlation metadata that lets one agent instance be joined to SDLC, lab, and trace evidence.
/// </summary>
public sealed record AgentRuntimeCorrelation
{
    /// <summary>
    /// Gets the SDLC story id that caused this instance to exist.
    /// </summary>
    public string? StoryId { get; init; }

    /// <summary>
    /// Gets the laboratory or workflow run id.
    /// </summary>
    public string? RunId { get; init; }

    /// <summary>
    /// Gets the experiment id when this instance is part of an experiment.
    /// </summary>
    public string? ExperimentId { get; init; }

    /// <summary>
    /// Gets the fleet id when this instance is part of an experiment fleet.
    /// </summary>
    public string? FleetId { get; init; }

    /// <summary>
    /// Gets the trace/correlation id for logs and distributed traces.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets additional non-secret correlation dimensions.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
