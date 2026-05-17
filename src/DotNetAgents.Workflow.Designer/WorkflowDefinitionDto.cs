using System.Text.Json.Serialization;

namespace DotNetAgents.Workflow.Designer;

/// <summary>
/// Data transfer object for workflow definitions in the visual designer.
/// </summary>
public class WorkflowDefinitionDto
{
    /// <summary>
    /// Gets or sets the workflow name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the workflow version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the list of nodes in the workflow.
    /// </summary>
    [JsonPropertyName("nodes")]
    public List<WorkflowNodeDto> Nodes { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of edges connecting nodes.
    /// </summary>
    [JsonPropertyName("edges")]
    public List<WorkflowEdgeDto> Edges { get; set; } = new();

    /// <summary>
    /// Gets or sets the entry point node name.
    /// </summary>
    [JsonPropertyName("entryPoint")]
    public string? EntryPoint { get; set; }

    /// <summary>
    /// Gets or sets the exit point node names.
    /// </summary>
    [JsonPropertyName("exitPoints")]
    public List<string> ExitPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the workflow metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Data transfer object for workflow nodes.
/// </summary>
public class WorkflowNodeDto
{
    /// <summary>
    /// Gets or sets the node ID (unique identifier).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node type (e.g., "function", "condition", "parallel", "human-in-loop").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Gets or sets the node position in the visual editor (X coordinate).
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the node position in the visual editor (Y coordinate).
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the node configuration/parameters.
    /// </summary>
    [JsonPropertyName("config")]
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary>
    /// Gets or sets the node label/display name.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets optional node description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Data transfer object for workflow edges.
/// </summary>
public class WorkflowEdgeDto
{
    /// <summary>
    /// Gets or sets the edge ID (unique identifier).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source node ID.
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target node ID.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the edge label/condition.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets whether this edge is conditional.
    /// </summary>
    [JsonPropertyName("conditional")]
    public bool Conditional { get; set; }

    /// <summary>
    /// Gets or sets the condition expression (if conditional).
    /// </summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
}

/// <summary>
/// Data transfer object for workflow execution status.
/// </summary>
public class WorkflowExecutionDto
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    [JsonPropertyName("executionId")]
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workflow name.
    /// </summary>
    [JsonPropertyName("workflowName")]
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Gets or sets the current node being executed.
    /// </summary>
    [JsonPropertyName("currentNode")]
    public string? CurrentNode { get; set; }

    /// <summary>
    /// Gets or sets the execution start time.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the execution end time.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the execution result (if completed).
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets any error message (if failed).
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
