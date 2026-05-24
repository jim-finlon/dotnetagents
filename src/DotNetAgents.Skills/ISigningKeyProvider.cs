// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Skills;

/// <summary>
/// Abstraction over a private key store used by <see cref="CapabilityPackSigner"/>. Per SUC-08
/// follow-up SecurityNotes: the public signing API never accepts raw private-key material;
/// callers pass an opaque key reference (e.g. a CredentialsAgent identity-card id) and the
/// provider returns a <see cref="SignedPayload"/> for the supplied canonical bytes.
/// </summary>
/// <remarks>
/// <para>
/// Two implementations ship in this slice:
/// </para>
/// <list type="bullet">
///   <item><see cref="InMemorySigningKeyProvider"/> — generates an RSA-PSS-SHA256 keypair at
///     construction time and exposes the public key for verification. Used by the test suite
///     and for offline pack scoring; do NOT use in production.</item>
///   <item>The CredentialsAgent provider is intentionally out of scope here — it lands when
///     the CredentialsAgent MCP client wires <c>sign_agent_card</c> end-to-end. The follow-up
///     story for that bridge is tracked separately.</item>
/// </list>
/// </remarks>
public interface ISigningKeyProvider
{
    /// <summary>
    /// Sign the supplied canonical payload bytes using the key identified by
    /// <paramref name="keyRef"/>.
    /// </summary>
    /// <param name="keyRef">Opaque key reference. Format is implementation-specific.</param>
    /// <param name="canonicalPayload">UTF-8 bytes to sign (already canonicalised by the caller).</param>
    SignedPayload Sign(string keyRef, ReadOnlySpan<byte> canonicalPayload);

    /// <summary>
    /// Verify a signature produced by <see cref="Sign"/> against <paramref name="canonicalPayload"/>.
    /// </summary>
    bool Verify(string keyRef, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature, string alg);
}

/// <summary>One signature produced by <see cref="ISigningKeyProvider.Sign"/>.</summary>
/// <param name="Alg">Algorithm identifier matching the schema enum (<c>ed25519</c> | <c>rsa-pss-sha256</c>).</param>
/// <param name="Signature">Base64-encoded signature bytes (matches the schema regex <c>^[A-Za-z0-9+/]+=*$</c>).</param>
public sealed record SignedPayload(string Alg, string Signature);
