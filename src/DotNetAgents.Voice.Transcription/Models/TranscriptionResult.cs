namespace DotNetAgents.Voice.Transcription.Models;

/// <summary>
/// Represents the result of a voice transcription operation.
/// </summary>
public record TranscriptionResult
{
    /// <summary>
    /// Gets the transcribed text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the detected language code (e.g., "en", "es", "fr").
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Gets the duration of the audio file.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the confidence score of the transcription (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Gets additional metadata about the transcription.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Gets the source file path.
    /// </summary>
    public string? SourceFilePath { get; init; }

    /// <summary>
    /// Gets the timestamp when transcription completed.
    /// </summary>
    public DateTime TranscribedAt { get; init; } = DateTime.UtcNow;
}
