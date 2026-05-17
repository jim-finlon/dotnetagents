using DotNetAgents.Voice.Transcription;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Voice.Transcription;

/// <summary>
/// Extension methods for registering voice transcription services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds voice transcription services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVoiceTranscription(
        this IServiceCollection services,
        Action<WhisperTranscriptionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new WhisperTranscriptionOptions();
        configure?.Invoke(options);

        // Register transcription service
        services.TryAddScoped<IVoiceTranscriptionService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WhisperTranscriptionService>>();
            var fileSystem = new System.IO.Abstractions.FileSystem();
            return new WhisperTranscriptionService(logger, fileSystem, options);
        });

        // Register file watcher
        services.TryAddSingleton<IVoiceFileWatcher, VoiceFileWatcher>();

        return services;
    }
}
