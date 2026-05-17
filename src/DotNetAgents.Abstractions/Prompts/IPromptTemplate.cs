namespace DotNetAgents.Abstractions.Prompts;

/// <summary>
/// Interface for prompt templates that support variable substitution.
/// </summary>
public interface IPromptTemplate
{
    /// <summary>
    /// Formats the template with the provided variables.
    /// </summary>
    /// <param name="variables">Dictionary of variable names to values.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The formatted prompt string.</returns>
    /// <exception cref="ArgumentException">Thrown when required variables are missing.</exception>
    Task<string> FormatAsync(
        IDictionary<string, object> variables,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the set of variable names required by this template.
    /// </summary>
    IReadOnlySet<string> Variables { get; }

    /// <summary>
    /// Gets the raw template string.
    /// </summary>
    string Template { get; }
}
