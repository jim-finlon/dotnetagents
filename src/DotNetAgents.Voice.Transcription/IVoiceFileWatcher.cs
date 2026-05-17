namespace DotNetAgents.Voice.Transcription;

/// <summary>
/// Event arguments for audio file detection.
/// </summary>
public class AudioFileDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the detected audio file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the timestamp when the file was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for watching folders for audio files.
/// </summary>
public interface IVoiceFileWatcher
{
    /// <summary>
    /// Event raised when a new audio file is detected.
    /// </summary>
    event EventHandler<AudioFileDetectedEventArgs> AudioFileDetected;

    /// <summary>
    /// Starts watching the specified folder for audio files.
    /// </summary>
    /// <param name="folderPath">The path to the folder to watch.</param>
    void StartWatching(string folderPath);

    /// <summary>
    /// Stops watching for audio files.
    /// </summary>
    void StopWatching();

    /// <summary>
    /// Gets a value indicating whether the watcher is currently active.
    /// </summary>
    bool IsWatching { get; }
}
