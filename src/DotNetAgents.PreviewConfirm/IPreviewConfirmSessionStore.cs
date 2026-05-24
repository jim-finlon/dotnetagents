// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.PreviewConfirm;

/// <summary>Persistence for preview/confirm sessions (in-memory, Redis, or DB).</summary>
public interface IPreviewConfirmSessionStore
{
    ValueTask SaveAsync(PreviewConfirmSession session, CancellationToken cancellationToken = default);

    ValueTask<PreviewConfirmSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Remove after successful apply or explicit cancel.</summary>
    ValueTask<bool> TryRemoveAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
