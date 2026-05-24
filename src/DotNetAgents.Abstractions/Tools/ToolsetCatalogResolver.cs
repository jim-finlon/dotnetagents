// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Tools;

public sealed class ToolsetCatalogResolver
{
    private readonly IReadOnlyDictionary<string, ToolDefinition> _tools;
    private readonly IReadOnlyDictionary<string, ToolsetDefinition> _toolsets;

    public ToolsetCatalogResolver(
        IEnumerable<ToolDefinition> tools,
        IEnumerable<ToolsetDefinition> toolsets)
    {
        _tools = tools.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _toolsets = toolsets.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ToolsetResolutionResult Resolve(ToolsetResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.RequestedToolset))
            throw new ArgumentException("Requested toolset is required.", nameof(request));

        var included = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var excluded = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ExpandToolset(request.RequestedToolset, included, excluded, aliases, []);

        var effective = new List<ToolDefinition>();
        var unavailable = new List<ToolAvailabilityResult>();
        var denied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var toolName in included)
        {
            var resolvedName = ResolveAlias(toolName, aliases);
            if (excluded.Contains(toolName) || excluded.Contains(resolvedName))
            {
                denied[resolvedName] = "Excluded by toolset policy.";
                continue;
            }

            if (!_tools.TryGetValue(resolvedName, out var tool))
            {
                unavailable.Add(new ToolAvailabilityResult(resolvedName, false, "Tool is not registered in the catalog."));
                continue;
            }

            var denial = GetDenialReason(tool, request);
            if (denial is not null)
            {
                denied[tool.Name] = denial;
                continue;
            }

            var missing = GetMissingDependency(tool, request);
            if (missing is not null)
            {
                unavailable.Add(new ToolAvailabilityResult(tool.Name, false, missing));
                continue;
            }

            effective.Add(tool);
        }

        return new ToolsetResolutionResult
        {
            RequestedToolset = request.RequestedToolset,
            EffectiveTools = effective.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            UnavailableTools = unavailable.OrderBy(x => x.ToolName, StringComparer.OrdinalIgnoreCase).ToList(),
            DeniedReasons = denied.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(StringComparer.OrdinalIgnoreCase),
            AliasExpansions = aliases.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(StringComparer.OrdinalIgnoreCase)
        };
    }

    public static IReadOnlyList<ToolsetDefinition> CreateDefaultToolsets() =>
    [
        new ToolsetDefinition
        {
            Name = "docs_only",
            IncludeTools = ["docs.search", "docs.read"],
            ExcludeTools = ["shell.exec", "git.push", "credentials.get"]
        },
        new ToolsetDefinition
        {
            Name = "coding_worktree",
            IncludeToolsets = ["docs_only"],
            IncludeTools = ["git.status", "dotnet.test", "worktree.assert"],
            ExcludeTools = ["credentials.get", "deploy.production"]
        },
        new ToolsetDefinition
        {
            Name = "gateway_chat",
            IncludeTools = ["gateway.send_message", "gateway.read_thread"],
            Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["send"] = "gateway.send_message"
            }
        },
        new ToolsetDefinition
        {
            Name = "scheduled_audit",
            IncludeToolsets = ["docs_only"],
            IncludeTools = ["planning_tools.list_stories", "knowledge-memory.search_lessons"],
            ExcludeTools = ["schedule.create", "deploy.production"]
        }
    ];

    private void ExpandToolset(
        string toolsetName,
        SortedSet<string> included,
        SortedSet<string> excluded,
        Dictionary<string, string> aliases,
        IReadOnlyList<string> stack)
    {
        if (stack.Contains(toolsetName, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Toolset cycle detected: {string.Join(" -> ", stack)} -> {toolsetName}");
        if (!_toolsets.TryGetValue(toolsetName, out var toolset))
            throw new KeyNotFoundException($"Toolset '{toolsetName}' is not registered in the catalog.");

        var nextStack = stack.Append(toolsetName).ToList();
        foreach (var nested in toolset.IncludeToolsets)
            ExpandToolset(nested, included, excluded, aliases, nextStack);
        foreach (var pair in toolset.Aliases)
            aliases[pair.Key] = pair.Value;
        foreach (var tool in toolset.IncludeTools)
            included.Add(ResolveAlias(tool, aliases));
        foreach (var tool in toolset.ExcludeTools)
            excluded.Add(ResolveAlias(tool, aliases));
    }

    private static string ResolveAlias(string toolName, IReadOnlyDictionary<string, string> aliases) =>
        aliases.TryGetValue(toolName, out var resolved) ? resolved : toolName;

    private static string? GetDenialReason(ToolDefinition tool, ToolsetResolutionRequest request)
    {
        if (tool.AllowedActorIds.Count > 0 && !tool.AllowedActorIds.Contains(request.ActorId))
            return $"Tool is not allowed for actor '{request.ActorId}'.";
        if (tool.AllowedChannels.Count > 0 && !tool.AllowedChannels.Contains(request.Channel))
            return $"Tool is not allowed on channel '{request.Channel}'.";
        if (!string.IsNullOrWhiteSpace(request.RequiredCapability) && !tool.Capabilities.Contains(request.RequiredCapability))
            return $"Tool does not provide required capability '{request.RequiredCapability}'.";
        return null;
    }

    private static string? GetMissingDependency(ToolDefinition tool, ToolsetResolutionRequest request)
    {
        var missingCredential = tool.RequiredCredentials.FirstOrDefault(x => !request.AvailableCredentials.Contains(x));
        if (missingCredential is not null)
            return $"Missing credential '{missingCredential}'.";
        var missingBinary = tool.RequiredBinaries.FirstOrDefault(x => !request.AvailableBinaries.Contains(x));
        if (missingBinary is not null)
            return $"Missing binary '{missingBinary}'.";
        return null;
    }
}
