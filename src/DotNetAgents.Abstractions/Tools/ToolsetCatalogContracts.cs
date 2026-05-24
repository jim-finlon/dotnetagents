// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace DotNetAgents.Abstractions.Tools;

public sealed record ToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlySet<string> Capabilities { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AllowedActorIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AllowedChannels { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> RequiredCredentials { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> RequiredBinaries { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ToolSchemaOverride? SchemaOverride { get; init; }
}

public sealed record ToolsetDefinition
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> IncludeToolsets { get; init; } = [];
    public IReadOnlyList<string> IncludeTools { get; init; } = [];
    public IReadOnlyList<string> ExcludeTools { get; init; } = [];
    public IReadOnlyDictionary<string, string> Aliases { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public ToolPolicy Policy { get; init; } = new();
}

public sealed record ToolPolicy
{
    public IReadOnlySet<string> AllowedActorTypes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AllowedChannels { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> EnvironmentTags { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record ToolAvailabilityResult(
    string ToolName,
    bool IsAvailable,
    string? Reason = null);

public sealed record ToolSchemaOverride(
    string ToolName,
    JsonElement InputSchema);

public sealed record ToolsetResolutionRequest
{
    public string ActorId { get; init; } = string.Empty;
    public string ActorType { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string RequestedToolset { get; init; } = string.Empty;
    public string? RequiredCapability { get; init; }
    public IReadOnlySet<string> EnvironmentTags { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AvailableCredentials { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AvailableBinaries { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record ToolsetResolutionResult
{
    public string RequestedToolset { get; init; } = string.Empty;
    public IReadOnlyList<ToolDefinition> EffectiveTools { get; init; } = [];
    public IReadOnlyList<ToolAvailabilityResult> UnavailableTools { get; init; } = [];
    public IReadOnlyDictionary<string, string> DeniedReasons { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> AliasExpansions { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
