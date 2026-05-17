namespace DotNetAgents.Abstractions.OutputParsers;

/// <summary>
/// Interface for parsing LLM output into structured formats.
/// </summary>
/// <typeparam name="T">The type to parse the output into.</typeparam>
public interface IOutputParser<T>
{
    /// <summary>
    /// Parses the raw output from an LLM into a structured format.
    /// </summary>
    /// <param name="output">The raw output string from the LLM.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The parsed output.</returns>
    /// <exception cref="ParsingException">Thrown when parsing fails.</exception>
    Task<T> ParseAsync(string output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets instructions for the LLM on how to format its output.
    /// </summary>
    /// <returns>Format instructions as a string.</returns>
    string GetFormatInstructions();
}

/// <summary>
/// Exception thrown when parsing fails.
/// </summary>
public class ParsingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParsingException"/> class.
    /// </summary>
    public ParsingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParsingException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ParsingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParsingException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
