// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Tools;

namespace DotNetAgents.Runtime;

public sealed class DictionaryToolsetResolver : IToolsetResolver
{
    private readonly IReadOnlyDictionary<string, ITool> _tools;

    public DictionaryToolsetResolver(IEnumerable<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        _tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ITool?> ResolveAsync(
        string toolName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _tools.TryGetValue(toolName, out var tool);
        return Task.FromResult(tool);
    }
}
