using System.Text.Json;

namespace DotNetAgents.LaneOps;

/// <summary>One operator-auditable recovery harness record.</summary>
public sealed record RecoveryHarnessAuditRecord(
    string? StoryId,
    string? LaneId,
    string? ActorId,
    string Mode,
    string RefusalReason,
    string VerifyState,
    int? ExitCode,
    DateTimeOffset OccurredAtUtc);

public interface IRecoveryHarnessAuditSink
{
    Task RecordAsync(RecoveryHarnessAuditRecord record, CancellationToken cancellationToken = default);
}

public sealed class NullRecoveryHarnessAuditSink : IRecoveryHarnessAuditSink
{
    public static readonly NullRecoveryHarnessAuditSink Instance = new();

    private NullRecoveryHarnessAuditSink()
    {
    }

    public Task RecordAsync(RecoveryHarnessAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryRecoveryHarnessAuditSink : IRecoveryHarnessAuditSink
{
    private readonly List<RecoveryHarnessAuditRecord> _records = new();
    private readonly object _lock = new();

    public Task RecordAsync(RecoveryHarnessAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_lock)
        {
            _records.Add(record);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<RecoveryHarnessAuditRecord> Snapshot()
    {
        lock (_lock)
        {
            return _records.ToArray();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _records.Count;
            }
        }
    }
}

/// <summary>Simple NDJSON audit sink for operator-visible lane recovery logs.</summary>
public sealed class FileRecoveryHarnessAuditSink(string logPath) : IRecoveryHarnessAuditSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _logPath = string.IsNullOrWhiteSpace(logPath)
        ? throw new ArgumentException("logPath is required.", nameof(logPath))
        : logPath;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public async Task RecordAsync(RecoveryHarnessAuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine;

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(_logPath, line, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }
}
