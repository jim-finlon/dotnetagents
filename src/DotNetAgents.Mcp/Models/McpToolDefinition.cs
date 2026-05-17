namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Represents an MCP tool definition.
/// </summary>
public record McpToolDefinition
{
    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the tool.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the input schema for the tool (JSON Schema format).
    /// </summary>
    public required McpToolInputSchema InputSchema { get; init; }

    /// <summary>
    /// Gets the name of the MCP service that provides this tool.
    /// </summary>
    public string? ServiceName { get; init; }

    /// <summary>
    /// Gets optional usage examples (e.g. sample inputs or call patterns).
    /// </summary>
    public IReadOnlyList<string>? Examples { get; init; }

    /// <summary>
    /// Gets optional category for grouping related tools (e.g. "Knowledge", "Session").
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets optional list of tool names commonly used together with this tool.
    /// </summary>
    public IReadOnlyList<string>? RelatedTools { get; init; }

    /// <summary>
    /// Gets optional guidance on when and why to use this tool.
    /// </summary>
    public string? UsageGuidance { get; init; }
}

/// <summary>
/// Represents the input schema for an MCP tool (JSON Schema format).
/// </summary>
public record McpToolInputSchema
{
    /// <summary>
    /// Gets the type of the schema (typically "object").
    /// </summary>
    public string Type { get; init; } = "object";

    /// <summary>
    /// Gets the properties of the input schema.
    /// </summary>
    public Dictionary<string, McpProperty> Properties { get; init; } = new();

    /// <summary>
    /// Gets the list of required property names.
    /// </summary>
    public List<string> Required { get; init; } = new();
}

/// <summary>
/// Represents a property definition in a tool schema.
/// </summary>
public record McpProperty
{
    /// <summary>
    /// Gets the type of the property (e.g., "string", "number", "boolean", "array", "object").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the description of the property.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the enum values if the property is an enum.
    /// </summary>
    public List<string>? Enum { get; init; }

    /// <summary>
    /// Gets the default value for the property.
    /// </summary>
    public object? Default { get; init; }

    /// <summary>
    /// When this property's <see cref="Type"/> is <c>"array"</c>, describes the schema
    /// of each element. Mirrors the JSON Schema <c>items</c> keyword and lets MCP
    /// clients (and the generated bridge / tool-search projections) tell an
    /// object-array apart from a string-array. Story c09513a4 traced repeated
    /// failed-call round trips to a missing <c>items</c> declaration; downstream
    /// callers can now publish the same shape the runtime accepts.
    /// </summary>
    public McpProperty? Items { get; init; }

    /// <summary>
    /// When this property's <see cref="Type"/> is <c>"object"</c>, the nested
    /// property dictionary. Optional; useful for documenting object-array element
    /// shapes via <see cref="Items"/>.
    /// </summary>
    public Dictionary<string, McpProperty>? Properties { get; init; }

    /// <summary>
    /// Required field names for an <see cref="Type"/> = <c>"object"</c> property
    /// (typically the element of an object array).
    /// </summary>
    public List<string>? Required { get; init; }
}
