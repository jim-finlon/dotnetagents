namespace DotNetAgents.Mcp.Exceptions;

/// <summary>
/// Exception thrown when an MCP operation fails.
/// </summary>
public class McpException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public McpException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public McpException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
