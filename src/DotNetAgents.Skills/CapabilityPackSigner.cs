// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;

namespace DotNetAgents.Skills;

/// <summary>
/// Signs and verifies <see cref="CapabilityPack"/> instances per the
/// <c>dna.skill.capability-pack.v1</c> <c>provenance.signedBy[]</c> contract. The signature
/// payload is the canonical pack JSON with <c>provenance.signedBy</c> excluded so adding a
/// second signer doesn't shift the bytes the first signer covered.
/// </summary>
/// <remarks>
/// <para>
/// SUC-08 follow-up (<c>77536c5f</c>). The signer never accepts raw private-key material;
/// callers pass an opaque key reference and an <see cref="ISigningKeyProvider"/> handles the
/// crypto. The in-memory provider that ships in this slice is for tests and offline scoring;
/// production signing flows through CredentialsAgent (separate follow-up).
/// </para>
/// <para>
/// Canonicalisation rule: serialize the pack via <see cref="CapabilityPackEmitter.JsonOpts"/>
/// with <see cref="CapabilityPack.Provenance"/> rewritten to drop the existing
/// <see cref="CapabilityPackProvenance.SignedBy"/> entries (other provenance fields preserved).
/// Result is UTF-8 encoded.
/// </para>
/// </remarks>
public static class CapabilityPackSigner
{
    /// <summary>
    /// Return a copy of <paramref name="pack"/> with one additional signature in
    /// <c>provenance.signedBy[]</c>. Existing signatures are preserved so multi-signer flows
    /// (e.g. operator + counter-agent) accumulate.
    /// </summary>
    /// <param name="pack">Pack to sign. Treated as immutable; caller receives a new instance.</param>
    /// <param name="actorId">Stable actor id (e.g. <c>agent-alpha</c>).</param>
    /// <param name="keyRef">Opaque key reference resolved by <paramref name="keyProvider"/>.</param>
    /// <param name="keyProvider">Key custody abstraction.</param>
    /// <param name="signedAtUtc">Optional ISO-8601 timestamp; default is <c>UtcNow.ToString("O")</c>.</param>
    public static CapabilityPack Sign(
        CapabilityPack pack,
        string actorId,
        string keyRef,
        ISigningKeyProvider keyProvider,
        DateTimeOffset? signedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyRef);
        ArgumentNullException.ThrowIfNull(keyProvider);

        var payload = CanonicalPayloadBytes(pack);
        var signed = keyProvider.Sign(keyRef, payload);

        var existing = pack.Provenance?.SignedBy ?? Array.Empty<CapabilityPackSignature>();
        var newSig = new CapabilityPackSignature(
            ActorId: actorId,
            Alg: signed.Alg,
            KeyRef: keyRef,
            Signature: signed.Signature,
            SignedAtUtc: (signedAtUtc ?? DateTimeOffset.UtcNow).ToString("O", System.Globalization.CultureInfo.InvariantCulture));

        var updatedSigs = new List<CapabilityPackSignature>(existing.Count + 1);
        updatedSigs.AddRange(existing);
        updatedSigs.Add(newSig);
        var updatedProv = (pack.Provenance ?? new CapabilityPackProvenance()) with { SignedBy = updatedSigs };
        return pack with { Provenance = updatedProv };
    }

    /// <summary>
    /// Verify every entry in <paramref name="pack"/>'s <c>provenance.signedBy[]</c>. Returns one
    /// outcome per signature so callers can identify which signer drifted.
    /// </summary>
    public static CapabilityPackVerificationResult Verify(
        CapabilityPack pack,
        ISigningKeyProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(keyProvider);

        var signatures = pack.Provenance?.SignedBy ?? Array.Empty<CapabilityPackSignature>();
        if (signatures.Count == 0)
        {
            return new CapabilityPackVerificationResult(
                AllValid: false,
                Outcomes: Array.Empty<CapabilityPackSignatureOutcome>(),
                Reason: "no_signatures");
        }

        var payload = CanonicalPayloadBytes(pack);
        var outcomes = new List<CapabilityPackSignatureOutcome>(signatures.Count);
        foreach (var sig in signatures)
        {
            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(sig.Signature);
            }
            catch (FormatException)
            {
                outcomes.Add(new CapabilityPackSignatureOutcome(
                    ActorId: sig.ActorId,
                    KeyRef: sig.KeyRef,
                    Valid: false,
                    Reason: "signature_base64_invalid"));
                continue;
            }
            var valid = keyProvider.Verify(sig.KeyRef, payload, signatureBytes, sig.Alg);
            outcomes.Add(new CapabilityPackSignatureOutcome(
                ActorId: sig.ActorId,
                KeyRef: sig.KeyRef,
                Valid: valid,
                Reason: valid ? "ok" : "signature_mismatch_or_unknown_key"));
        }
        return new CapabilityPackVerificationResult(
            AllValid: outcomes.All(o => o.Valid),
            Outcomes: outcomes,
            Reason: outcomes.All(o => o.Valid) ? "ok" : "one_or_more_signatures_invalid");
    }

    /// <summary>
    /// Build the canonical bytes that signatures cover. Equivalent to serialising
    /// <paramref name="pack"/> with <c>provenance.signedBy</c> removed (other provenance fields
    /// preserved). UTF-8 encoded; trailing newline stripped to keep the signed bytes stable
    /// regardless of how <see cref="CapabilityPackEmitter.Serialize"/> shapes its tail.
    /// </summary>
    public static byte[] CanonicalPayloadBytes(CapabilityPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        // Strip signedBy so the canonical payload is identical no matter how many signers have
        // already attached. Keep other provenance fields so a signature commits to source repo
        // ref / extraction context. If the provenance carries ONLY signedBy (no source repo /
        // lesson ref), collapse it back to null so the serialised JSON omits the provenance
        // block entirely — otherwise an empty signedBy:[] would diverge from the pre-sign view
        // and break verification.
        CapabilityPackProvenance? canonicalProvenance = pack.Provenance switch
        {
            null => null,
            { SourceRepoRef: null, ExtractedFromLessonRef: null } => null,
            var p => p with { SignedBy = null },
        };
        var canonicalPack = pack with { Provenance = canonicalProvenance };
        var json = JsonSerializer.Serialize(canonicalPack, CapabilityPackEmitter.JsonOpts);
        // Strip a trailing newline if present so callers that re-emit with/without one produce
        // the same signed bytes.
        if (json.EndsWith('\n')) json = json[..^1];
        return Encoding.UTF8.GetBytes(json);
    }
}

/// <summary>Outcome of <see cref="CapabilityPackSigner.Verify"/>.</summary>
public sealed record CapabilityPackVerificationResult(
    bool AllValid,
    IReadOnlyList<CapabilityPackSignatureOutcome> Outcomes,
    string Reason);

/// <summary>Per-signature outcome inside a <see cref="CapabilityPackVerificationResult"/>.</summary>
public sealed record CapabilityPackSignatureOutcome(
    string ActorId,
    string KeyRef,
    bool Valid,
    string Reason);
