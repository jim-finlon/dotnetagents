// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Memory.Advanced;

/// <summary>Procedure (sequence of steps). FR-MEM-003.</summary>
public sealed record Procedure
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public IReadOnlyList<Step> Steps { get; init; } = Array.Empty<Step>();
}

/// <summary>Single step in a procedure. FR-MEM-003.</summary>
public sealed record Step
{
    public int Order { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Action { get; init; }
}
