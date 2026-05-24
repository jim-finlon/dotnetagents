// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace DotNetAgents.Security.Audit;

/// <summary>
/// Console-based implementation of <see cref="IAuditLogger"/>.
/// </summary>
public class ConsoleAuditLogger : IAuditLogger
{
    private readonly ILogger<ConsoleAuditLogger>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleAuditLogger"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for audit events.</param>
    public ConsoleAuditLogger(ILogger<ConsoleAuditLogger>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task LogAuditEventAsync(
        AuditEventType eventType,
        string message,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        cancellationToken.ThrowIfCancellationRequested();

        var logLevel = GetLogLevel(eventType);
        var metadataString = metadata != null && metadata.Count > 0
            ? $" Metadata: {System.Text.Json.JsonSerializer.Serialize(metadata)}"
            : string.Empty;

        var auditMessage = $"[AUDIT] {eventType}: {message}{metadataString}";

        _logger?.Log(logLevel, auditMessage);
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {auditMessage}");

        return Task.CompletedTask;
    }

    private static LogLevel GetLogLevel(AuditEventType eventType)
    {
        return eventType switch
        {
            AuditEventType.SecurityEvent => LogLevel.Warning,
            AuditEventType.RateLimitExceeded => LogLevel.Warning,
            AuditEventType.PromptInjectionDetected => LogLevel.Error,
            AuditEventType.SensitiveDataDetected => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}
