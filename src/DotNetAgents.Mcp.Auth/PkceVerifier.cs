// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;

namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Server-side PKCE verification helper. Compares a presented <c>code_verifier</c> against a
/// stored <c>code_challenge</c> using <see cref="CryptographicOperations.FixedTimeEquals"/> so
/// timing leaks cannot be used to brute-force the verifier.
/// </summary>
public static class PkceVerifier
{
    /// <summary>
    /// Returns <c>true</c> when the verifier produces a matching SHA-256 challenge. The
    /// comparison is constant-time and rejects any non-S256 method.
    /// </summary>
    public static bool Verify(string presentedVerifier, string expectedChallenge, string method = "S256")
    {
        if (!string.Equals(method, PkceParameters.SupportedCodeChallengeMethod, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(presentedVerifier) || string.IsNullOrWhiteSpace(expectedChallenge))
        {
            return false;
        }

        if (presentedVerifier.Length is < PkceParameters.MinVerifierLength or > PkceParameters.MaxVerifierLength)
        {
            return false;
        }

        var actual = PkceCodeChallengeFactory.ChallengeForVerifier(presentedVerifier);
        var actualBytes = System.Text.Encoding.ASCII.GetBytes(actual);
        var expectedBytes = System.Text.Encoding.ASCII.GetBytes(expectedChallenge);

        if (actualBytes.Length != expectedBytes.Length) return false;
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
