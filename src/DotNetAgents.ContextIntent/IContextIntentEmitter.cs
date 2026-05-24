// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.ContextIntent;

/// <summary>
/// Emits a <see cref="ContextIntentEnvelope"/> at a handoff boundary (story claim, lane
/// assignment, code review, deploy approval, MCP tool dispatch). Implementations route
/// the envelope into evidence storage (workflow orchestrator contribution_entry) and notify any
/// registered <see cref="IContextIntentConsumer"/> at the receive boundary.
/// </summary>
public interface IContextIntentEmitter
{
    /// <summary>
    /// Emit the envelope at the named handoff event. Returns the persisted envelope (with
    /// any system-stamped fields populated, e.g. captured_at if not already set).
    /// </summary>
    Task<ContextIntentEnvelope> EmitAsync(
        string handoffEvent,
        ContextIntentEnvelope envelope,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Consumes a <see cref="ContextIntentEnvelope"/> at the receive side of a handoff. Returns
/// an acknowledgment receipt the emitter can store as evidence that the receiver registered
/// the intent.
/// </summary>
public interface IContextIntentConsumer
{
    /// <summary>
    /// Acknowledge the envelope. Implementations may reject (return non-IsAccepted result)
    /// when the envelope fails validation or violates a local policy. Default acceptance
    /// is required behavior; rejection is opt-in.
    /// </summary>
    Task<ContextIntentAcknowledgmentReceipt> ConsumeAsync(
        ContextIntentEnvelope envelope,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The receipt the consumer issues to the emitter — proof of receive-side registration.
/// </summary>
public sealed record ContextIntentAcknowledgmentReceipt(
    string ReceiptId,
    string TaskId,
    string ConsumerId,
    bool IsAccepted,
    DateTimeOffset AcknowledgedAtUtc,
    IReadOnlyList<string>? RejectionReasons = null);
