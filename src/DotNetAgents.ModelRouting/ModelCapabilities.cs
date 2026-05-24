// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ModelRouting;

/// <summary>Capabilities advertised by a model (e.g. vision, function_calling). FR-MR-002.</summary>
public sealed record ModelCapabilities
{
    /// <summary>Model identifier.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Optional endpoint URL.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Set of capability names this model supports.</summary>
    public IReadOnlySet<string> Capabilities { get; init; } = new HashSet<string>();

    /// <summary>Whether the model is currently available (e.g. not overloaded).</summary>
    public bool Available { get; init; } = true;
}
