// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.A2A;

/// <summary>Agent card describing capabilities. FR-A2A-001.</summary>
public sealed record AgentCard
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<Skill> Skills { get; init; } = Array.Empty<Skill>();
    public IReadOnlyList<string> SupportedModes { get; init; } = Array.Empty<string>();
    public string Version { get; init; } = "1.0";
}
