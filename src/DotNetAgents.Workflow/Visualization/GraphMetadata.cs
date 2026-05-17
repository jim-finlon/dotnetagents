namespace DotNetAgents.Workflow.Visualization;

/// <summary>
/// Metadata about a workflow graph for serialization and visualization.
/// </summary>
public record GraphMetadata
{
    /// <summary>
    /// Gets or sets the entry point node name.
    /// </summary>
    public string? EntryPoint { get; init; }

    /// <summary>
    /// Gets or sets the exit point node names.
    /// </summary>
    public IReadOnlyList<string> ExitPoints { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the node metadata.
    /// </summary>
    public IReadOnlyList<NodeMetadata> Nodes { get; init; } = Array.Empty<NodeMetadata>();

    /// <summary>
    /// Gets or sets the edge metadata.
    /// </summary>
    public IReadOnlyList<EdgeMetadata> Edges { get; init; } = Array.Empty<EdgeMetadata>();
}

/// <summary>
/// Metadata about a workflow node.
/// </summary>
public record NodeMetadata
{
    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the node description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Metadata about a workflow edge.
/// </summary>
public record EdgeMetadata
{
    /// <summary>
    /// Gets or sets the source node name.
    /// </summary>
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the target node name.
    /// </summary>
    public string To { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the edge description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets whether the edge is conditional.
    /// </summary>
    public bool IsConditional { get; init; }
}
