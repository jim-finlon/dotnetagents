using DotNetAgents.Voice.Transcription.Models;

namespace DotNetAgents.Voice.Transcription;

/// <summary>
/// Interface for voice transcription services.
/// </summary>
public interface IVoiceTranscriptionService
{
    /// <summary>
    /// Transcribes an audio file.
    /// </summary>
    /// <param name="audioFilePath">The path to the audio file.</param>
    /// <param name="options">Optional transcription options.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The transcription result.</returns>
    Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath,
        TranscriptionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes audio from a stream.
    /// </summary>
    /// <param name="audioStream">The audio stream.</param>
    /// <param name="options">Optional transcription options.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The transcription result.</returns>
    Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        TranscriptionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the supported audio formats.
    /// </summary>
    /// <returns>The list of supported file extensions (e.g., ".mp3", ".wav").</returns>
    IReadOnlyList<string> GetSupportedFormats();
}
