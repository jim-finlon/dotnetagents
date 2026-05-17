namespace DotNetAgents.Mcp.Models;

/// <summary>
/// Represents a request to list tools from an MCP service.
/// </summary>
public record McpListToolsRequest
{
    /// <summary>
    /// Gets the optional category filter for tools.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the optional limit on the number of tools to return.
    /// </summary>
    public int? Limit { get; init; }
}

/// <summary>
/// Represents the response from listing tools.
/// </summary>
public record McpListToolsResponse
{
    /// <summary>
    /// Gets the list of tool definitions.
    /// </summary>
    public required List<McpToolDefinition> Tools { get; init; }

    /// <summary>
    /// Gets the total count of tools available.
    /// </summary>
    public int TotalCount { get; init; }
}
