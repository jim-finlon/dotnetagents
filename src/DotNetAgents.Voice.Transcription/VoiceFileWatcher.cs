using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Transcription;

/// <summary>
/// Watches a folder for new audio files.
/// </summary>
public class VoiceFileWatcher : IVoiceFileWatcher, IDisposable
{
    private readonly ILogger<VoiceFileWatcher> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IReadOnlyList<string> _supportedFormats;
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<AudioFileDetectedEventArgs>? AudioFileDetected;

    /// <inheritdoc />
    public bool IsWatching => _watcher != null;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceFileWatcher"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="supportedFormats">The list of supported audio file extensions.</param>
    public VoiceFileWatcher(
        ILogger<VoiceFileWatcher> logger,
        IFileSystem fileSystem,
        IReadOnlyList<string>? supportedFormats = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _supportedFormats = supportedFormats ?? new[] { ".mp3", ".wav", ".m4a", ".flac", ".ogg", ".webm" };
    }

    /// <inheritdoc />
    public void StartWatching(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        if (_watcher != null)
        {
            _logger.LogWarning("File watcher is already active");
            return;
        }

        if (!_fileSystem.Directory.Exists(folderPath))
        {
            _fileSystem.Directory.CreateDirectory(folderPath);
            _logger.LogInformation("Created watch directory: {Path}", folderPath);
        }

        _watcher = new FileSystemWatcher(folderPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            Filter = "*.*",
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
        _watcher.Changed += OnFileChanged;

        _logger.LogInformation("Started watching folder: {Path}", folderPath);
    }

    /// <inheritdoc />
    public void StopWatching()
    {
        if (_watcher == null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileCreated;
        _watcher.Changed -= OnFileChanged;
        _watcher.Dispose();
        _watcher = null;

        _logger.LogInformation("Stopped watching folder");
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        HandleFileEvent(e.FullPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Only handle if file is fully written (not still being written)
        HandleFileEvent(e.FullPath);
    }

    private void HandleFileEvent(string filePath)
    {
        try
        {
            var extension = _fileSystem.Path.GetExtension(filePath).ToLowerInvariant();
            if (!_supportedFormats.Contains(extension))
            {
                return;
            }

            // Wait a bit to ensure file is fully written
            Thread.Sleep(500);

            if (!_fileSystem.File.Exists(filePath))
            {
                return;
            }

            _logger.LogInformation("Detected audio file: {FilePath}", filePath);

            AudioFileDetected?.Invoke(
                this,
                new AudioFileDetectedEventArgs { FilePath = filePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file event: {FilePath}", filePath);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopWatching();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
