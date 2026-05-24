// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.Memory;

namespace DotNetAgents.Core.Memory.Implementations;

/// <summary>
/// In-memory implementation of <see cref="IMemory"/> that stores messages in memory.
/// </summary>
public class InMemoryMemory : IMemory
{
    private readonly List<MemoryMessage> _messages = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task AddMessageAsync(
        MemoryMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        lock (_lock)
        {
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (count < 0)
            throw new ArgumentException("Count must be non-negative.", nameof(count));

        lock (_lock)
        {
            var messages = _messages
                .TakeLast(count)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<MemoryMessage>>(messages);
        }
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _messages.Clear();
        }

        return Task.CompletedTask;
    }
}
