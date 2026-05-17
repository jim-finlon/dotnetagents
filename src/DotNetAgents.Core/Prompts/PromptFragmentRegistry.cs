using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

namespace DotNetAgents.Core.Prompts;

/// <summary>
/// Role of a prompt fragment within a composed prompt route.
/// </summary>
public enum PromptFragmentRole
{
    System,
    Developer,
    User,
    ToolPolicy
}

/// <summary>
/// Versioned, reusable prompt fragment. Content is intentionally separate from telemetry metadata.
/// </summary>
/// <param name="Id">Stable fragment id, such as <c>planning_tools.core.v1</c>.</param>
/// <param name="Content">Prompt text used at runtime. Never emit this value to telemetry.</param>
/// <param name="Role">Prompt role used by the host runtime.</param>
/// <param name="Version">Optional semver or date-stamped version.</param>
public sealed record PromptFragment(
    string Id,
    string Content,
    PromptFragmentRole Role = PromptFragmentRole.System,
    string? Version = null);

/// <summary>
/// Route from an agent/lane key to ordered prompt fragment ids.
/// </summary>
/// <param name="AgentKey">Stable agent or service key.</param>
/// <param name="RouteKey">Lane, workflow phase, or task-family route.</param>
/// <param name="FragmentIds">Ordered fragment ids. Order is composition order.</param>
public sealed record PromptFragmentRoute(
    string AgentKey,
    string RouteKey,
    IReadOnlyList<string> FragmentIds);

/// <summary>
/// Non-secret metadata emitted for composed prompt fragments.
/// </summary>
public sealed record PromptFragmentReference(
    string Id,
    string Hash,
    PromptFragmentRole Role,
    string? Version);

/// <summary>
/// Result of composing a route's fragments.
/// </summary>
public sealed record ComposedPrompt(
    string AgentKey,
    string RouteKey,
    string Content,
    IReadOnlyList<PromptFragmentReference> Fragments);

/// <summary>
/// In-memory registry for keyed, versioned prompt fragments and route composition.
/// </summary>
public sealed class PromptFragmentRegistry
{
    private readonly Dictionary<string, PromptFragment> _fragments = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PromptFragmentRoute> _routes = new(StringComparer.Ordinal);
    private readonly ILogger<PromptFragmentRegistry>? _logger;

    public PromptFragmentRegistry(ILogger<PromptFragmentRegistry>? logger = null)
        => _logger = logger;

    public IReadOnlyCollection<PromptFragment> Fragments => _fragments.Values;

    public IReadOnlyCollection<PromptFragmentRoute> Routes => _routes.Values;

    public PromptFragmentRegistry RegisterFragment(PromptFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        EnsureNonEmpty(fragment.Id, nameof(fragment.Id));
        EnsureNonEmpty(fragment.Content, nameof(fragment.Content));

        if (_fragments.ContainsKey(fragment.Id))
        {
            throw new InvalidOperationException($"Prompt fragment id '{fragment.Id}' is already registered.");
        }

        _fragments.Add(fragment.Id, fragment);
        return this;
    }

    public PromptFragmentRegistry RegisterRoute(PromptFragmentRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        EnsureNonEmpty(route.AgentKey, nameof(route.AgentKey));
        EnsureNonEmpty(route.RouteKey, nameof(route.RouteKey));

        if (route.FragmentIds.Count == 0)
        {
            throw new ArgumentException("Route must reference at least one prompt fragment.", nameof(route));
        }

        var routeKey = BuildRouteKey(route.AgentKey, route.RouteKey);
        if (_routes.ContainsKey(routeKey))
        {
            throw new InvalidOperationException($"Prompt route '{route.AgentKey}/{route.RouteKey}' is already registered.");
        }

        ValidateFragmentIds(route.FragmentIds);
        _routes.Add(routeKey, route);
        return this;
    }

    public void ValidateRoutes()
    {
        foreach (var route in _routes.Values)
        {
            ValidateFragmentIds(route.FragmentIds);
        }
    }

    public ComposedPrompt Compose(string agentKey, string routeKey, string separator = "\n\n")
    {
        EnsureNonEmpty(agentKey, nameof(agentKey));
        EnsureNonEmpty(routeKey, nameof(routeKey));

        if (!_routes.TryGetValue(BuildRouteKey(agentKey, routeKey), out var route))
        {
            throw new KeyNotFoundException($"Prompt route '{agentKey}/{routeKey}' is not registered.");
        }

        var fragments = route.FragmentIds.Select(id => _fragments[id]).ToArray();
        var content = string.Join(separator, fragments.Select(f => f.Content));
        var references = fragments.Select(ToReference).ToArray();

        _logger?.LogInformation(
            "Composed prompt route {AgentKey}/{RouteKey} with prompt fragments {PromptFragmentIds} and hashes {PromptFragmentHashes}",
            agentKey,
            routeKey,
            references.Select(f => f.Id).ToArray(),
            references.Select(f => f.Hash).ToArray());

        return new ComposedPrompt(agentKey, routeKey, content, references);
    }

    private static PromptFragmentReference ToReference(PromptFragment fragment)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(fragment.Content));
        return new PromptFragmentReference(
            fragment.Id,
            Convert.ToHexString(bytes).ToLowerInvariant(),
            fragment.Role,
            fragment.Version);
    }

    private void ValidateFragmentIds(IReadOnlyList<string> fragmentIds)
    {
        foreach (var fragmentId in fragmentIds)
        {
            EnsureNonEmpty(fragmentId, nameof(fragmentIds));
            if (!_fragments.ContainsKey(fragmentId))
            {
                throw new KeyNotFoundException($"Prompt fragment id '{fragmentId}' is not registered.");
            }
        }
    }

    private static string BuildRouteKey(string agentKey, string routeKey)
        => $"{agentKey}\u001f{routeKey}";

    private static void EnsureNonEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must be non-empty.", name);
        }
    }
}
