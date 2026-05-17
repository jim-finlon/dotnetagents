using DotNetAgents.Agents.Registry;

namespace DotNetAgents.Agents.Hierarchical;

/// <summary>
/// Manages hierarchical organization of agents (teams, departments, organizations).
/// </summary>
public interface IHierarchicalAgentOrganization
{
    /// <summary>
    /// Gets the root organization node.
    /// </summary>
    OrganizationNode Root { get; }

    /// <summary>
    /// Creates a new organization node (team, department, etc.).
    /// </summary>
    /// <param name="name">The name of the organization node.</param>
    /// <param name="parentId">Optional parent node ID. If null, creates a root node.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created organization node.</returns>
    Task<OrganizationNode> CreateNodeAsync(
        string name,
        string? parentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an agent to an organization node.
    /// </summary>
    /// <param name="nodeId">The organization node ID.</param>
    /// <param name="agentId">The agent ID to add.</param>
    /// <param name="role">Optional role for the agent in this organization.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAgentToNodeAsync(
        string nodeId,
        string agentId,
        string? role = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agents in an organization node and its children.
    /// </summary>
    /// <param name="nodeId">The organization node ID.</param>
    /// <param name="includeChildren">Whether to include child nodes.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>List of agent information.</returns>
    Task<IReadOnlyList<AgentInfo>> GetAgentsInNodeAsync(
        string nodeId,
        bool includeChildren = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the organization hierarchy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The organization tree.</returns>
    Task<OrganizationTree> GetHierarchyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a node in the organization hierarchy.
/// </summary>
public class OrganizationNode
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent node ID, or null if this is the root.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the list of child node IDs.
    /// </summary>
    public List<string> ChildIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of agent IDs in this node.
    /// </summary>
    public List<string> AgentIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the node type (team, department, organization, etc.).
    /// </summary>
    public OrganizationNodeType Type { get; set; }

    /// <summary>
    /// Gets or sets optional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of organization nodes.
/// </summary>
public enum OrganizationNodeType
{
    /// <summary>
    /// Root organization.
    /// </summary>
    Organization,

    /// <summary>
    /// Department or division.
    /// </summary>
    Department,

    /// <summary>
    /// Team or group.
    /// </summary>
    Team,

    /// <summary>
    /// Custom node type.
    /// </summary>
    Custom
}

/// <summary>
/// Represents the organization tree structure.
/// </summary>
public class OrganizationTree
{
    /// <summary>
    /// Gets or sets the root node.
    /// </summary>
    public OrganizationNode Root { get; set; } = null!;

    /// <summary>
    /// Gets or sets all nodes in the tree, keyed by node ID.
    /// </summary>
    public Dictionary<string, OrganizationNode> AllNodes { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of agents in the organization.
    /// </summary>
    public int TotalAgents { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth of the hierarchy.
    /// </summary>
    public int MaxDepth { get; set; }
}
