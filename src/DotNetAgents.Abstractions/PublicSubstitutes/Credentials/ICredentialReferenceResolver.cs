// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.PublicSubstitutes.Credentials;

/// <summary>
/// Resolves a public credential reference into a temporary accessor. The resolver
/// may read from local development sources, premium plugins, or private factory
/// services, but callers consume only this public contract.
/// </summary>
public interface ICredentialReferenceResolver
{
    /// <summary>Resolve a reference into an accessor without exposing raw secret values.</summary>
    ValueTask<ICredentialAccessor> ResolveAsync(
        CredentialReference reference,
        CancellationToken cancellationToken = default);
}
