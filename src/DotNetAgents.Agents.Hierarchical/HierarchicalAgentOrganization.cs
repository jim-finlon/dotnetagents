using DotNetAgents.Agents.Registry;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Agents.Hierarchical;

/// <summary>
/// Implements hierarchical organization of agents.
/// </summary>
public class HierarchicalAgentOrganization : IHierarchicalAgentOrganization
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<HierarchicalAgentOrganization>? _logger;
    private readonly Dictionary<string, OrganizationNode> _nodes = new();
    private OrganizationNode? _root;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HierarchicalAgentOrganization"/> class.
    /// </summary>
    /// <param name="agentRegistry">The agent registry.</param>
    /// <param name="logger">Optional logger instance.</param>
    public HierarchicalAgentOrganization(
        IAgentRegistry agentRegistry,
        ILogger<HierarchicalAgentOrganization>? logger = null)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _logger = logger;
    }

    /// <inheritdoc />
    public OrganizationNode Root
    {
        get
        {
            lock (_lock)
            {
                if (_root == null)
                {
                    _root = new OrganizationNode
                    {
                        Id = "root",
                        Name = "Root Organization",
                        Type = OrganizationNodeType.Organization
                    };
                    _nodes["root"] = _root;
                }
                return _root;
            }
        }
    }

    /// <inheritdoc />
    public Task<OrganizationNode> CreateNodeAsync(
        string name,
        string? parentId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var nodeId = Guid.NewGuid().ToString();
            var node = new OrganizationNode
            {
                Id = nodeId,
                Name = name,
                ParentId = parentId,
                Type = OrganizationNodeType.Team
            };

            _nodes[nodeId] = node;

            // Add to parent's children
            if (parentId != null && _nodes.TryGetValue(parentId, out var parent))
            {
                parent.ChildIds.Add(nodeId);
            }
            else if (parentId == null)
            {
                // Add to root
                Root.ChildIds.Add(nodeId);
                node.ParentId = Root.Id;
            }

            _logger?.LogInformation("Created organization node {NodeId} ({Name}) under {ParentId}",
                nodeId, name, parentId ?? "root");

            return Task.FromResult(node);
        }
    }

    /// <inheritdoc />
    public Task AddAgentToNodeAsync(
        string nodeId,
        string agentId,
        string? role = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeId);
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (_nodes.TryGetValue(nodeId, out var node))
            {
                if (!node.AgentIds.Contains(agentId))
                {
                    node.AgentIds.Add(agentId);

                    if (!string.IsNullOrEmpty(role))
                    {
                        node.Metadata[$"agent:{agentId}:role"] = role;
                    }

                    _logger?.LogInformation("Added agent {AgentId} to organization node {NodeId}",
                        agentId, nodeId);
                }
            }
            else
            {
                throw new ArgumentException($"Organization node '{nodeId}' not found.", nameof(nodeId));
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentInfo>> GetAgentsInNodeAsync(
        string nodeId,
        bool includeChildren = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeId);
        cancellationToken.ThrowIfCancellationRequested();

        var agentIds = new HashSet<string>();

        lock (_lock)
        {
            if (_nodes.TryGetValue(nodeId, out var node))
            {
                foreach (var agentId in node.AgentIds)
                {
                    agentIds.Add(agentId);
                }

                if (includeChildren)
                {
                    CollectAgentIdsRecursive(node, agentIds);
                }
            }
        }

        var allAgents = await _agentRegistry.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return allAgents.Where(a => agentIds.Contains(a.AgentId)).ToList();
    }

    /// <inheritdoc />
    public Task<OrganizationTree> GetHierarchyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            var totalAgents = _nodes.Values.Sum(n => n.AgentIds.Count);
            var maxDepth = CalculateMaxDepth(Root);

            return Task.FromResult(new OrganizationTree
            {
                Root = Root,
                AllNodes = new Dictionary<string, OrganizationNode>(_nodes),
                TotalAgents = totalAgents,
                MaxDepth = maxDepth
            });
        }
    }

    private void CollectAgentIdsRecursive(OrganizationNode node, HashSet<string> agentIds)
    {
        foreach (var agentId in node.AgentIds)
        {
            agentIds.Add(agentId);
        }

        lock (_lock)
        {
            foreach (var childId in node.ChildIds)
            {
                if (_nodes.TryGetValue(childId, out var child))
                {
                    CollectAgentIdsRecursive(child, agentIds);
                }
            }
        }
    }

    private int CalculateMaxDepth(OrganizationNode node)
    {
        if (node.ChildIds.Count == 0)
            return 1;

        lock (_lock)
        {
            var maxChildDepth = node.ChildIds
                .Select(childId => _nodes.TryGetValue(childId, out var child) ? CalculateMaxDepth(child) : 0)
                .DefaultIfEmpty(0)
                .Max();

            return 1 + maxChildDepth;
        }
    }
}
