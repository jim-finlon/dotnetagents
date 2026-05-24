// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.A2A;

/// <summary>Base implementation: card from constructor, HandleTaskAsync dispatches by skill to a delegate; StreamTaskAsync yields one completion event. FR-A2A-002.</summary>
public abstract class A2AAgentBase : IA2AAgent
{
    private readonly AgentCard _card;
    private readonly Func<A2ATask, CancellationToken, Task<A2AResponse>> _handleTask;

    protected A2AAgentBase(AgentCard card, Func<A2ATask, CancellationToken, Task<A2AResponse>> handleTask)
    {
        _card = card ?? throw new ArgumentNullException(nameof(card));
        _handleTask = handleTask ?? throw new ArgumentNullException(nameof(handleTask));
    }

    /// <inheritdoc />
    public AgentCard GetAgentCard() => _card;

    /// <inheritdoc />
    public async Task<A2AResponse> HandleTaskAsync(A2ATask task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        return await _handleTask(task, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<A2AEvent> StreamTaskAsync(A2ATask task, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        var response = await HandleTaskAsync(task, cancellationToken).ConfigureAwait(false);
        yield return new A2AEvent { TaskId = task.Id, EventType = "completed", Payload = response };
    }
}
