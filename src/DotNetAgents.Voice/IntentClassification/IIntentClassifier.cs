namespace DotNetAgents.Voice.IntentClassification;

/// <summary>
/// Interface for classifying voice commands into structured intents.
/// </summary>
public interface IIntentClassifier
{
    /// <summary>
    /// Classifies a voice command into a structured intent.
    /// </summary>
    /// <param name="commandText">The raw voice command text.</param>
    /// <param name="context">Optional context for context-aware classification.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The classified intent.</returns>
    /// <exception cref="ArgumentException">Thrown when command text is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when classification fails.</exception>
    Task<Intent> ClassifyAsync(
        string commandText,
        IntentContext? context = null,
        CancellationToken cancellationToken = default);
}
