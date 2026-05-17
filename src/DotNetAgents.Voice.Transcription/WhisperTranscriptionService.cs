using System.Diagnostics;
using System.IO.Abstractions;
using DotNetAgents.Voice.Transcription.Models;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Voice.Transcription;

/// <summary>
/// Whisper-based transcription service using Python subprocess.
/// </summary>
public class WhisperTranscriptionService : IVoiceTranscriptionService
{
    private readonly ILogger<WhisperTranscriptionService> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly WhisperTranscriptionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhisperTranscriptionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="options">The Whisper transcription options.</param>
    public WhisperTranscriptionService(
        ILogger<WhisperTranscriptionService> logger,
        IFileSystem fileSystem,
        WhisperTranscriptionOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedFormats()
    {
        return new[] { ".mp3", ".wav", ".m4a", ".flac", ".ogg", ".webm" };
    }

    /// <inheritdoc />
    public async Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath,
        TranscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioFilePath);

        if (!_fileSystem.File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        var format = _fileSystem.Path.GetExtension(audioFilePath).ToLowerInvariant();
        if (!GetSupportedFormats().Contains(format))
        {
            throw new NotSupportedException($"Audio format '{format}' is not supported");
        }

        _logger.LogInformation("Transcribing audio file: {FilePath}", audioFilePath);

        var transcriptionOptions = options ?? new TranscriptionOptions();
        var model = transcriptionOptions.Model ?? _options.DefaultModel;
        var language = transcriptionOptions.Language ?? "auto";

        try
        {
            // Create temporary output file
            var tempOutputFile = _fileSystem.Path.Combine(
                _fileSystem.Path.GetTempPath(),
                $"transcription_{Guid.NewGuid()}.json");

            // Build Whisper command
            var whisperArgs = new List<string>
            {
                audioFilePath,
                "--model", model,
                "--output_format", "json",
                "--output_dir", _fileSystem.Path.GetDirectoryName(tempOutputFile)!,
                "--language", language
            };

            if (transcriptionOptions.Temperature > 0)
            {
                whisperArgs.AddRange(new[] { "--temperature", transcriptionOptions.Temperature.ToString("F2") });
            }

            // Execute Whisper
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _options.WhisperCommand ?? "whisper",
                Arguments = string.Join(" ", whisperArgs.Select(arg => $"\"{arg}\"")),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set working directory if specified
            if (!string.IsNullOrEmpty(_options.WorkingDirectory))
            {
                processStartInfo.WorkingDirectory = _options.WorkingDirectory;
            }

            // Set environment variables if specified
            if (_options.EnvironmentVariables != null)
            {
                foreach (var (key, value) in _options.EnvironmentVariables)
                {
                    processStartInfo.EnvironmentVariables[key] = value;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start Whisper process");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken)
                    .ConfigureAwait(false);
                _logger.LogError("Whisper transcription failed: {Error}", error);
                throw new InvalidOperationException($"Whisper transcription failed: {error}");
            }

            // Read output JSON file
            var outputFile = _fileSystem.Path.Combine(
                _fileSystem.Path.GetDirectoryName(tempOutputFile)!,
                _fileSystem.Path.GetFileNameWithoutExtension(audioFilePath) + ".json");

            if (!_fileSystem.File.Exists(outputFile))
            {
                throw new FileNotFoundException($"Whisper output file not found: {outputFile}");
            }

            var jsonContent = await _fileSystem.File.ReadAllTextAsync(outputFile, cancellationToken)
                .ConfigureAwait(false);

            // Parse Whisper JSON output (simplified - actual format may vary)
            var result = ParseWhisperOutput(jsonContent, audioFilePath, stopwatch.Elapsed);

            // Cleanup temp file
            try
            {
                if (_fileSystem.File.Exists(outputFile))
                {
                    _fileSystem.File.Delete(outputFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp file: {File}", outputFile);
            }

            _logger.LogInformation(
                "Transcription completed: {FilePath} in {Duration}ms",
                audioFilePath,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transcribe audio file: {FilePath}", audioFilePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        TranscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioStream);

        // Write stream to temporary file
        var tempFile = _fileSystem.Path.Combine(
            _fileSystem.Path.GetTempPath(),
            $"audio_{Guid.NewGuid()}.tmp");

        try
        {
            await using (var fileStream = _fileSystem.File.Create(tempFile))
            {
                await audioStream.CopyToAsync(fileStream, cancellationToken)
                    .ConfigureAwait(false);
            }

            return await TranscribeAsync(tempFile, options, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            // Cleanup temp file
            try
            {
                if (_fileSystem.File.Exists(tempFile))
                {
                    _fileSystem.File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp file: {File}", tempFile);
            }
        }
    }

    private static TranscriptionResult ParseWhisperOutput(
        string jsonContent,
        string sourceFilePath,
        TimeSpan duration)
    {
        // Simplified parsing - actual Whisper JSON format may vary
        // This is a placeholder that should be replaced with proper JSON deserialization
        using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        var text = root.TryGetProperty("text", out var textElement)
            ? textElement.GetString() ?? string.Empty
            : string.Empty;

        var language = root.TryGetProperty("language", out var langElement)
            ? langElement.GetString() ?? "en"
            : "en";

        return new TranscriptionResult
        {
            Text = text,
            Language = language,
            Duration = duration,
            Confidence = 0.95, // Default confidence
            SourceFilePath = sourceFilePath,
            Metadata = new Dictionary<string, object>
            {
                ["model"] = "whisper",
                ["format"] = "json"
            }
        };
    }
}

/// <summary>
/// Options for Whisper transcription service.
/// </summary>
public class WhisperTranscriptionOptions
{
    /// <summary>
    /// Gets or sets the Whisper command to execute (default: "whisper").
    /// </summary>
    public string WhisperCommand { get; set; } = "whisper";

    /// <summary>
    /// Gets or sets the default model to use.
    /// </summary>
    public string DefaultModel { get; set; } = "base";

    /// <summary>
    /// Gets or sets the working directory for Whisper execution.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets environment variables to set for Whisper process.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
}
