namespace DotNetAgents.Abstractions.Exceptions;

/// <summary>
/// Base exception class for all DotNetAgents-related exceptions.
/// </summary>
public class AgentException : Exception
{
    /// <summary>
    /// Gets the correlation ID associated with this exception.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the category of the error.
    /// </summary>
    public ErrorCategory Category { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentException"/> class.
    /// </summary>
    public AgentException()
        : base()
    {
        Category = ErrorCategory.Unknown;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AgentException(string message)
        : base(message)
    {
        Category = ErrorCategory.Unknown;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AgentException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Category = ErrorCategory.Unknown;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentException"/> class with a specified error message, category, and correlation ID.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="category">The category of the error.</param>
    /// <param name="correlationId">The correlation ID associated with this exception.</param>
    public AgentException(string message, ErrorCategory category, string? correlationId = null)
        : base(message)
    {
        Category = category;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentException"/> class with a specified error message, category, correlation ID, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="category">The category of the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="correlationId">The correlation ID associated with this exception.</param>
    public AgentException(string message, ErrorCategory category, Exception? innerException, string? correlationId = null)
        : base(message, innerException)
    {
        Category = category;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Categories of errors that can occur in DotNetAgents.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Unknown error category.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Error related to LLM operations.
    /// </summary>
    LLMError = 1,

    /// <summary>
    /// Error related to tool execution.
    /// </summary>
    ToolError = 2,

    /// <summary>
    /// Error related to workflow execution.
    /// </summary>
    WorkflowError = 3,

    /// <summary>
    /// Error related to configuration.
    /// </summary>
    ConfigurationError = 4,

    /// <summary>
    /// Error related to security.
    /// </summary>
    SecurityError = 5,
}
