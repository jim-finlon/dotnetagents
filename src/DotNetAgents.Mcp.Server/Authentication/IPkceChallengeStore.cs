// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Server-side store for PKCE code-challenge values that have been bound to authorization
/// codes. The token-exchange endpoint pulls the stored challenge for an authorization code,
/// verifies the presented verifier against it, and removes the entry so each authorization
/// code is single-use.
/// </summary>
/// <remarks>
/// The default in-memory store is fine for single-replica deployments. Multi-replica
/// services need a shared store (Redis, EF, etc.) so a code issued on replica A can be
/// redeemed on replica B.
/// </remarks>
public interface IPkceChallengeStore
{
    /// <summary>Bind a code_challenge to an authorization code. Overwrites any existing entry for the same code.</summary>
    Task StoreAsync(string authorizationCode, PkceChallengeRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically take and remove the record for an authorization code. Returns <c>null</c> when
    /// the code is unknown or already consumed; this prevents replay.
    /// </summary>
    Task<PkceChallengeRecord?> ConsumeAsync(string authorizationCode, CancellationToken cancellationToken = default);
}

/// <param name="CodeChallenge">The S256 challenge the authorize step recorded.</param>
/// <param name="CodeChallengeMethod">Method advertised; DNA refuses anything other than S256.</param>
/// <param name="ClientId">CIMD URL or legacy client id used at authorize time.</param>
/// <param name="ExpiresAtUtc">When this entry expires (RFC 7636 §4.4).</param>
public sealed record PkceChallengeRecord(
    string CodeChallenge,
    string CodeChallengeMethod,
    string ClientId,
    DateTimeOffset ExpiresAtUtc);
