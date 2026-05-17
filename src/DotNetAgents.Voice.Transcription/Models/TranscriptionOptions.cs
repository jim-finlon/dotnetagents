namespace DotNetAgents.Voice.Transcription.Models;

/// <summary>
/// Options for voice transcription.
/// </summary>
public record TranscriptionOptions
{
    /// <summary>
    /// Gets or sets the Whisper model to use (tiny, base, small, medium, large).
    /// </summary>
    public string Model { get; init; } = "base";

    /// <summary>
    /// Gets or sets the language code (e.g., "en", "es", "fr"). If null, language is auto-detected.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets or sets whether to include word-level timestamps.
    /// </summary>
    public bool IncludeTimestamps { get; init; } = false;

    /// <summary>
    /// Gets or sets the temperature for transcription (0.0 to 1.0).
    /// </summary>
    public double Temperature { get; init; } = 0.0;

    /// <summary>
    /// Gets or sets additional options specific to the transcription engine.
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; init; } = new();
}
