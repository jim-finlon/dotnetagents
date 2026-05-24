// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text;

namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Generates RFC 7636 PKCE parameters: a cryptographically-random <c>code_verifier</c> and the
/// matching SHA-256 <c>code_challenge</c>.
/// </summary>
/// <remarks>
/// Verifier length defaults to 64 characters (well above the RFC 7636 minimum of 43 and well
/// below the maximum of 128). Verifier alphabet is the unreserved character set:
/// <c>[A-Za-z0-9-._~]</c>. Output is URL-safe so it can be passed verbatim in query strings.
/// </remarks>
public static class PkceCodeChallengeFactory
{
    private const string UnreservedAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

    /// <summary>
    /// Generate fresh PKCE parameters with a verifier of the requested length.
    /// </summary>
    /// <param name="verifierLength">Verifier length in characters; default 64. Must be in [43, 128] per RFC 7636.</param>
    public static PkceParameters Generate(int verifierLength = 64)
    {
        if (verifierLength is < PkceParameters.MinVerifierLength or > PkceParameters.MaxVerifierLength)
        {
            throw new ArgumentOutOfRangeException(nameof(verifierLength),
                $"Verifier length must be between {PkceParameters.MinVerifierLength} and {PkceParameters.MaxVerifierLength} per RFC 7636.");
        }

        Span<byte> randomBytes = stackalloc byte[verifierLength];
        RandomNumberGenerator.Fill(randomBytes);
        var verifier = new char[verifierLength];
        for (var i = 0; i < verifierLength; i++)
        {
            verifier[i] = UnreservedAlphabet[randomBytes[i] % UnreservedAlphabet.Length];
        }

        var verifierString = new string(verifier);
        return new PkceParameters(verifierString, ChallengeForVerifier(verifierString));
    }

    /// <summary>
    /// Compute the SHA-256-base64url <c>code_challenge</c> for an existing <paramref name="verifier"/>.
    /// Useful for tests and for reissuing the challenge from a stored verifier.
    /// </summary>
    public static string ChallengeForVerifier(string verifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifier);
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64Url.Encode(bytes);
    }
}

/// <summary>Minimal RFC 7515 base64url encode/decode without padding.</summary>
internal static class Base64Url
{
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        var standard = Convert.ToBase64String(bytes);
        return standard.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static byte[] Decode(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        var standard = input.Replace('-', '+').Replace('_', '/');
        var padding = (4 - (standard.Length % 4)) % 4;
        return Convert.FromBase64String(standard + new string('=', padding));
    }
}
