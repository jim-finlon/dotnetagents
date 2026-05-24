// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using DotNetAgents.Abstractions.PublicSubstitutes.Tasks;

namespace DotNetAgents.Tasks.PublicSubstitutes.Tasks;

/// <summary>
/// Single-process JSONL-backed public task store. It appends task snapshots for
/// developer convenience and does not provide distributed storage guarantees.
/// </summary>
public sealed class FileBackedTaskStore : IPublicTaskStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly InMemoryTaskStore _inner = new();
    private readonly string _filePath;

    public FileBackedTaskStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        _filePath = filePath;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    public async ValueTask<PublicTaskHandle> StartAsync(
        PublicTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var handle = await _inner.StartAsync(request, cancellationToken).ConfigureAwait(false);
        var record = await _inner.GetAsync(handle.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Task '{handle.Id}' was not stored after start.");

        await AppendAsync("task.started", record, cancellationToken).ConfigureAwait(false);
        return handle;
    }

    public async ValueTask CompleteAsync(
        PublicTaskHandle handle,
        PublicTaskOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        await _inner.CompleteAsync(handle, outcome, cancellationToken).ConfigureAwait(false);
        var record = await _inner.GetAsync(handle.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Task '{handle.Id}' was not stored after completion.");

        await AppendAsync("task.completed", record, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<PublicTaskRecord?> GetAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        _inner.GetAsync(id, cancellationToken);

    public IAsyncEnumerable<PublicTaskRecord> ListAsync(
        PublicTaskQuery? query = null,
        CancellationToken cancellationToken = default) =>
        _inner.ListAsync(query, cancellationToken);

    private async Task AppendAsync(
        string eventType,
        PublicTaskRecord record,
        CancellationToken cancellationToken)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            eventType,
            recordedAt = DateTimeOffset.UtcNow,
            task = record
        }, Json);

        await File.AppendAllTextAsync(_filePath, envelope + Environment.NewLine, cancellationToken).ConfigureAwait(false);
    }
}
