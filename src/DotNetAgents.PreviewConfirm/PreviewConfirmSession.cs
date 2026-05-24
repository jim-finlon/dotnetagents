// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.PreviewConfirm;

/// <summary>Lifecycle for a single preview/confirm gate.</summary>
public enum PreviewConfirmSessionState
{
    AwaitingConfirmation = 0,
    Confirmed = 1,
    Rejected = 2,
    Expired = 3
}

/// <summary>Durable-enough session record for an operator or agent to confirm a previewed mutation.</summary>
public sealed record PreviewConfirmSession(
    Guid SessionId,
    string ConfirmationToken,
    string PreviewPayload,
    PreviewConfirmSessionState State,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
