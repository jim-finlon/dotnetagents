using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

public enum UnifiedToolSource
{
    BuiltIn,
    Mcp,
    Plugin
}

public enum UnifiedToolLifecycleStatus
{
    Unknown = 0,
    Experimental = 1,
    Core = 2,
    Deprecated = 3
}

public enum UnifiedToolRiskTier
{
    Low = 0,
    Medium = 1,
    High = 2
}

public sealed record UnifiedToolAuthMetadata(
    string Type,
    IReadOnlyList<string> EnvVars,
    string? SetupDocs = null,
    string? SetupUrl = null);

public sealed record UnifiedToolDescriptor(
    string Name,
    string Description,
    UnifiedToolSource Source,
    string? ServiceName = null,
    IReadOnlyList<string>? Tags = null,
    string? Domain = null,
    UnifiedToolLifecycleStatus LifecycleStatus = UnifiedToolLifecycleStatus.Unknown,
    string? Owner = null,
    string? SupportChannel = null,
    UnifiedToolRiskTier RiskTier = UnifiedToolRiskTier.Low,
    bool ConfirmationRequired = false,
    IReadOnlyList<string>? AllowedRoles = null,
    UnifiedToolAuthMetadata? Auth = null);

public sealed record UnifiedToolCatalogManifest(
    int SchemaVersion,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<UnifiedToolDescriptor> Tools);

public interface IUnifiedToolRegistry
{
    Task RegisterAsync(UnifiedToolDescriptor tool, CancellationToken cancellationToken = default);
    Task RegisterManyAsync(IEnumerable<UnifiedToolDescriptor> tools, CancellationToken cancellationToken = default);
    Task RegisterMcpDefinitionsAsync(
        IEnumerable<McpToolDefinition> tools,
        string serviceName,
        UnifiedToolSource source = UnifiedToolSource.Mcp,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnifiedToolDescriptor>> ListAsync(CancellationToken cancellationToken = default);
    Task<UnifiedToolDescriptor?> FindByNameAsync(string toolName, CancellationToken cancellationToken = default);
}
