using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Workflow.Graph;

/// <summary>
/// Represents a state graph for defining workflows (LangGraph-like).
/// </summary>
/// <typeparam name="TState">The type of the workflow state. Must be a reference type.</typeparam>
public class StateGraph<TState> where TState : class
{
    private readonly Dictionary<string, GraphNode<TState>> _nodes = new();
    private readonly List<GraphEdge<TState>> _edges = new();
    private string? _entryPoint;
    private readonly HashSet<string> _exitPoints = new();

    /// <summary>
    /// Gets a read-only collection of all nodes in the graph.
    /// </summary>
    public IReadOnlyDictionary<string, GraphNode<TState>> Nodes => _nodes;

    /// <summary>
    /// Gets a read-only list of all edges in the graph.
    /// </summary>
    public IReadOnlyList<GraphEdge<TState>> Edges => _edges.AsReadOnly();

    /// <summary>
    /// Gets the entry point node name.
    /// </summary>
    public string? EntryPoint => _entryPoint;

    /// <summary>
    /// Gets a read-only set of exit point node names.
    /// </summary>
    public IReadOnlySet<string> ExitPoints => _exitPoints;

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <returns>The graph instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a node with the same name already exists.</exception>
    public StateGraph<TState> AddNode(GraphNode<TState> node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (_nodes.ContainsKey(node.Name))
        {
            throw new ArgumentException($"A node with the name '{node.Name}' already exists.", nameof(node));
        }

        _nodes[node.Name] = node;
        return this;
    }

    /// <summary>
    /// Adds a node to the graph using a handler function.
    /// </summary>
    /// <param name="name">The unique name of the node.</param>
    /// <param name="handler">The handler function that processes the state.</param>
    /// <returns>The graph instance for method chaining.</returns>
    public StateGraph<TState> AddNode(string name, Func<TState, CancellationToken, Task<TState>> handler)
    {
        var node = new GraphNode<TState>(name, handler);
        return AddNode(node);
    }

    /// <summary>
    /// Adds an edge between two nodes.
    /// </summary>
    /// <param name="edge">The edge to add.</param>
    /// <returns>The graph instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="edge"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the edge references non-existent nodes.</exception>
    public StateGraph<TState> AddEdge(GraphEdge<TState> edge)
    {
        if (edge == null)
            throw new ArgumentNullException(nameof(edge));

        if (!_nodes.ContainsKey(edge.From))
        {
            throw new ArgumentException($"Source node '{edge.From}' does not exist in the graph.", nameof(edge));
        }

        if (!_nodes.ContainsKey(edge.To))
        {
            throw new ArgumentException($"Target node '{edge.To}' does not exist in the graph.", nameof(edge));
        }

        _edges.Add(edge);
        return this;
    }

    /// <summary>
    /// Adds an edge between two nodes.
    /// </summary>
    /// <param name="from">The name of the source node.</param>
    /// <param name="to">The name of the target node.</param>
    /// <param name="condition">An optional condition function. If null, the edge is unconditional.</param>
    /// <returns>The graph instance for method chaining.</returns>
    public StateGraph<TState> AddEdge(string from, string to, Func<TState, bool>? condition = null)
    {
        var edge = new GraphEdge<TState>(from, to, condition);
        return AddEdge(edge);
    }

    /// <summary>
    /// Sets the entry point of the graph.
    /// </summary>
    /// <param name="nodeName">The name of the entry node.</param>
    /// <returns>The graph instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the node does not exist.</exception>
    public StateGraph<TState> SetEntryPoint(string nodeName)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
            throw new ArgumentException("Node name cannot be null or whitespace.", nameof(nodeName));

        if (!_nodes.ContainsKey(nodeName))
        {
            throw new ArgumentException($"Node '{nodeName}' does not exist in the graph.", nameof(nodeName));
        }

        _entryPoint = nodeName;
        return this;
    }

    /// <summary>
    /// Adds an exit point to the graph.
    /// </summary>
    /// <param name="nodeName">The name of the exit node.</param>
    /// <returns>The graph instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the node does not exist.</exception>
    public StateGraph<TState> AddExitPoint(string nodeName)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
            throw new ArgumentException("Node name cannot be null or whitespace.", nameof(nodeName));

        if (!_nodes.ContainsKey(nodeName))
        {
            throw new ArgumentException($"Node '{nodeName}' does not exist in the graph.", nameof(nodeName));
        }

        _exitPoints.Add(nodeName);
        return this;
    }

    /// <summary>
    /// Validates the graph structure.
    /// </summary>
    /// <exception cref="AgentException">Thrown when the graph is invalid.</exception>
    public void Validate()
    {
        var errors = new List<string>();

        if (_entryPoint == null)
        {
            errors.Add("Graph must have an entry point.");
        }

        if (_exitPoints.Count == 0)
        {
            errors.Add("Graph must have at least one exit point.");
        }

        // Check for unreachable nodes
        if (_entryPoint != null)
        {
            var reachable = GetReachableNodes(_entryPoint);
            var unreachable = _nodes.Keys.Except(reachable).ToList();
            if (unreachable.Count > 0)
            {
                errors.Add($"Unreachable nodes: {string.Join(", ", unreachable)}");
            }
        }

        // Check for nodes with no outgoing edges (except exit points)
        foreach (var node in _nodes.Keys)
        {
            if (!_exitPoints.Contains(node) && !_edges.Any(e => e.From == node))
            {
                errors.Add($"Node '{node}' has no outgoing edges and is not an exit point.");
            }
        }

        if (errors.Count > 0)
        {
            throw new AgentException(
                $"Graph validation failed: {string.Join(" ", errors)}",
                ErrorCategory.ConfigurationError);
        }
    }

    /// <summary>
    /// Gets all nodes reachable from the given starting node.
    /// </summary>
    /// <param name="startNode">The starting node name.</param>
    /// <returns>A set of reachable node names.</returns>
    private HashSet<string> GetReachableNodes(string startNode)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);

            var outgoingEdges = _edges.Where(e => e.From == current);
            foreach (var edge in outgoingEdges)
            {
                if (!visited.Contains(edge.To))
                {
                    queue.Enqueue(edge.To);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Gets all edges that can be taken from the given node given the current state.
    /// </summary>
    /// <param name="nodeName">The name of the node.</param>
    /// <param name="state">The current state.</param>
    /// <returns>A list of edges that can be taken.</returns>
    public IReadOnlyList<GraphEdge<TState>> GetOutgoingEdges(string nodeName, TState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        return _edges
            .Where(e => e.From == nodeName && e.ShouldTake(state))
            .ToList()
            .AsReadOnly();
    }
}
