using DotNetAgents.Voice.IntentClassification;

namespace DotNetAgents.Voice.Parsing;

/// <summary>
/// Interface for parsing voice commands into structured intents.
/// </summary>
public interface ICommandParser
{
    /// <summary>
    /// Parses a voice command into a structured intent.
    /// </summary>
    /// <param name="rawText">The raw voice command text.</param>
    /// <param name="context">Optional context for context-aware parsing.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The parsed intent.</returns>
    /// <exception cref="ArgumentException">Thrown when command text is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when parsing fails.</exception>
    Task<Intent> ParseAsync(
        string rawText,
        IntentContext? context = null,
        CancellationToken cancellationToken = default);
}
