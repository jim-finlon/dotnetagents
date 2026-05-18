using System.Collections.Concurrent;

namespace DotNetAgents.ContextIntent;

/// <summary>
/// Default in-memory implementation of <see cref="IContextIntentEmitter"/> + companion
/// in-memory consumer registry. Real deployment uses a service-side implementation that
/// persists envelopes into workflow orchestrator contribution_entry; this in-memory pair is for tests
/// and local development.
/// </summary>
public sealed class InMemoryContextIntentEmitter : IContextIntentEmitter
{
    private readonly ContextIntentValidator _validator;
    private readonly ContextIntentEnforcementMode _mode;
    private readonly IReadOnlyList<IContextIntentConsumer> _consumers;
    private readonly ConcurrentDictionary<string, ContextIntentEnvelope> _emitted = new();

    public InMemoryContextIntentEmitter(
        ContextIntentValidator? validator = null,
        ContextIntentEnforcementMode mode = ContextIntentEnforcementMode.WarnOnMissing,
        IEnumerable<IContextIntentConsumer>? consumers = null)
    {
        _validator = validator ?? new ContextIntentValidator();
        _mode = mode;
        _consumers = consumers?.ToArray() ?? Array.Empty<IContextIntentConsumer>();
    }

    /// <summary>Snapshot of envelopes emitted so far. Test-only convenience.</summary>
    public IReadOnlyDictionary<string, ContextIntentEnvelope> Emitted => _emitted;

    /// <summary>Receipts collected from consumers, indexed by receipt id.</summary>
    public IReadOnlyList<ContextIntentAcknowledgmentReceipt> Receipts { get; private set; } =
        Array.Empty<ContextIntentAcknowledgmentReceipt>();

    /// <inheritdoc />
    public async Task<ContextIntentEnvelope> EmitAsync(
        string handoffEvent,
        ContextIntentEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(handoffEvent);

        var validation = _validator.Validate(envelope);
        if (!validation.IsValid)
        {
            if (_mode == ContextIntentEnforcementMode.RequireEmission)
            {
                throw new ContextIntentValidationException(
                    $"Envelope rejected for handoff '{handoffEvent}': {string.Join("; ", validation.Errors)}",
                    validation);
            }
        }

        // Stamp captured_at if not already set.
        if (envelope.Provenance.CapturedAt == default)
        {
            envelope = envelope with
            {
                Provenance = envelope.Provenance with { CapturedAt = DateTimeOffset.UtcNow },
            };
        }

        _emitted[envelope.TaskId] = envelope;

        if (_consumers.Count > 0)
        {
            var ackTasks = _consumers.Select(c => c.ConsumeAsync(envelope, cancellationToken));
            var receipts = await Task.WhenAll(ackTasks).ConfigureAwait(false);
            Receipts = (Receipts ?? Array.Empty<ContextIntentAcknowledgmentReceipt>())
                .Concat(receipts).ToArray();
        }

        return envelope;
    }
}

/// <summary>
/// Thrown when validation fails under <see cref="ContextIntentEnforcementMode.RequireEmission"/>.
/// </summary>
public sealed class ContextIntentValidationException : Exception
{
    public ContextIntentValidationResult ValidationResult { get; }

    public ContextIntentValidationException(string message, ContextIntentValidationResult result)
        : base(message)
    {
        ValidationResult = result;
    }
}
