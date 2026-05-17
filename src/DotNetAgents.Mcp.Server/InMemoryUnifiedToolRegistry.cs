using System.Collections.Concurrent;
using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

public sealed class InMemoryUnifiedToolRegistry : IUnifiedToolRegistry
{
    private readonly ConcurrentDictionary<string, UnifiedToolDescriptor> _tools = new(StringComparer.OrdinalIgnoreCase);

    public Task RegisterAsync(UnifiedToolDescriptor tool, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tool);
        McpToolNameConvention.Validate(tool.Name, nameof(tool));
        _tools[tool.Name] = tool;
        return Task.CompletedTask;
    }

    public Task RegisterManyAsync(IEnumerable<UnifiedToolDescriptor> tools, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tools);
        foreach (var tool in tools)
        {
            ArgumentNullException.ThrowIfNull(tool);
            McpToolNameConvention.Validate(tool.Name, nameof(tools));
            _tools[tool.Name] = tool;
        }
        return Task.CompletedTask;
    }

    public Task RegisterMcpDefinitionsAsync(
        IEnumerable<McpToolDefinition> tools,
        string serviceName,
        UnifiedToolSource source = UnifiedToolSource.Mcp,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        foreach (var tool in tools)
        {
            ArgumentNullException.ThrowIfNull(tool);
            McpToolNameConvention.Validate(tool.Name, nameof(tools));
            var descriptor = new UnifiedToolDescriptor(
                tool.Name,
                tool.Description,
                source,
                serviceName);
            _tools[tool.Name] = descriptor;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UnifiedToolDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<UnifiedToolDescriptor> list = _tools.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<UnifiedToolDescriptor?> FindByNameAsync(string toolName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        _tools.TryGetValue(toolName, out var descriptor);
        return Task.FromResult(descriptor);
    }
}
