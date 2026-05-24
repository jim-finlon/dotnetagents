// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Abstractions.PublicSubstitutes.Credentials;

namespace DotNetAgents.Credentials.Client;

/// <summary>
/// Resolves credential references through a short-lived in-memory cache and
/// exposes explicit refresh hooks for credential-rotation aware callers.
/// </summary>
public interface IRotationAwareCredentialAccessor : ICredentialReferenceResolver
{
    /// <summary>
    /// Re-fetch a credential value and replace the cached copy for the supplied reference.
    /// </summary>
    ValueTask<ICredentialAccessor> RefreshAsync(
        CredentialReference reference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate the cached value for the supplied reference, if present.
    /// </summary>
    void Invalidate(CredentialReference reference);

    /// <summary>
    /// Invalidate and re-fetch after a downstream credential consumer reports
    /// <c>401</c> or <c>AUTH_FAILED</c>.
    /// </summary>
    ValueTask<ICredentialAccessor> RefreshAfterAuthenticationFailureAsync(
        CredentialReference reference,
        CancellationToken cancellationToken = default);
}
