namespace DotNetAgents.Security.Audit;

/// <summary>
/// Interface for audit logging of security-relevant events.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="eventType">The type of audit event.</param>
    /// <param name="message">The audit message.</param>
    /// <param name="metadata">Optional metadata associated with the event.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous audit logging operation.</returns>
    Task LogAuditEventAsync(
        AuditEventType eventType,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of audit events.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// An LLM API call was made.
    /// </summary>
    LLMCall = 1,

    /// <summary>
    /// A tool was executed.
    /// </summary>
    ToolExecution = 2,

    /// <summary>
    /// State was modified.
    /// </summary>
    StateModification = 3,

    /// <summary>
    /// Configuration was changed.
    /// </summary>
    ConfigurationChange = 4,

    /// <summary>
    /// A security event occurred.
    /// </summary>
    SecurityEvent = 5,

    /// <summary>
    /// A rate limit was exceeded.
    /// </summary>
    RateLimitExceeded = 6,

    /// <summary>
    /// Prompt injection was detected.
    /// </summary>
    PromptInjectionDetected = 7,

    /// <summary>
    /// Sensitive data was detected.
    /// </summary>
    SensitiveDataDetected = 8
}
