using DotNetAgents.Abstractions.Exceptions;

namespace DotNetAgents.Core.OutputParsers;

/// <summary>
/// Exception thrown when output parsing fails.
/// </summary>
public class ParsingException : AgentException
{
    /// <summary>
    /// Gets the raw output that failed to parse.
    /// </summary>
    public string? RawOutput { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParsingException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="rawOutput">The raw output that failed to parse.</param>
    /// <param name="innerException">The inner exception.</param>
    public ParsingException(string message, string? rawOutput = null, Exception? innerException = null)
        : base(message, ErrorCategory.ConfigurationError, innerException)
    {
        RawOutput = rawOutput;
    }
}
