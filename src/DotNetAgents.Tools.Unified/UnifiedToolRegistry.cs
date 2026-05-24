// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Tools;
using DotNetAgents.Mcp.Abstractions;
using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Tools.Unified;

public sealed record UnifiedToolDefinition(string Name, string Description, string Source);

public interface IUnifiedToolRegistry
{
    Task<IReadOnlyList<UnifiedToolDefinition>> GetAllToolsAsync(CancellationToken cancellationToken = default);
    Task<UnifiedToolDefinition?> FindAsync(string toolName, CancellationToken cancellationToken = default);
}

public sealed class UnifiedToolRegistry(
    IToolRegistry toolRegistry,
    IMcpToolRegistry mcpToolRegistry) : IUnifiedToolRegistry
{
    private readonly IToolRegistry _toolRegistry = toolRegistry;
    private readonly IMcpToolRegistry _mcpToolRegistry = mcpToolRegistry;

    public async Task<IReadOnlyList<UnifiedToolDefinition>> GetAllToolsAsync(CancellationToken cancellationToken = default)
    {
        var builtIn = _toolRegistry
            .GetAllTools()
            .Select(t => new UnifiedToolDefinition(t.Name, t.Description, "builtin"));

        var mcpTools = await _mcpToolRegistry.GetAllToolsAsync(cancellationToken).ConfigureAwait(false);
        var mcp = mcpTools.Select(ToUnified);

        return builtIn.Concat(mcp)
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<UnifiedToolDefinition?> FindAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return null;
        }

        var builtIn = _toolRegistry.GetTool(toolName);
        if (builtIn is not null)
        {
            return new UnifiedToolDefinition(builtIn.Name, builtIn.Description, "builtin");
        }

        var mcp = await _mcpToolRegistry.FindToolAsync(toolName, cancellationToken).ConfigureAwait(false);
        return mcp is null ? null : ToUnified(mcp);
    }

    private static UnifiedToolDefinition ToUnified(McpToolDefinition tool)
        => new(tool.Name, tool.Description, $"mcp:{tool.ServiceName ?? "unknown"}");
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAgentsUnifiedTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IUnifiedToolRegistry, UnifiedToolRegistry>();
        return services;
    }
}
