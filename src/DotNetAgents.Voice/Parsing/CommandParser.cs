using DotNetAgents.Voice.IntentClassification;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Parsing;

/// <summary>
/// Parses voice commands into structured intents using an intent classifier.
/// </summary>
public class CommandParser : ICommandParser
{
    private readonly IIntentClassifier _classifier;
    private readonly ILogger<CommandParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandParser"/> class.
    /// </summary>
    /// <param name="classifier">The intent classifier to use.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandParser(
        IIntentClassifier classifier,
        ILogger<CommandParser> logger)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Intent> ParseAsync(
        string rawText,
        IntentContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new ArgumentException("Command text cannot be null or empty", nameof(rawText));
        }

        _logger.LogInformation(
            "CommandParser: classifying textChars={TextChars} preview={Preview}",
            rawText.Length,
            rawText.Length > 200 ? rawText[..200] + "…" : rawText);

        try
        {
            var intent = await _classifier.ClassifyAsync(rawText, context, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "CommandParser: parsed intent={Intent} targetService={TargetService} tool={Tool} confidence={Confidence:F2}",
                intent.FullName,
                intent.TargetService,
                intent.Tool,
                intent.Confidence);

            return intent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse command: {RawText}", rawText);
            throw new InvalidOperationException($"Failed to parse command: {ex.Message}", ex);
        }
    }
}
