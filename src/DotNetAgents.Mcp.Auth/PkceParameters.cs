namespace DotNetAgents.Mcp.Auth;

/// <summary>
/// Parameters for the RFC 7636 PKCE flow as adopted by MCP November 2025: callers generate a
/// random <see cref="CodeVerifier"/>, hash with SHA-256 + base64url to produce
/// <see cref="CodeChallenge"/>, present the challenge during the authorize step, and present
/// the verifier during the token exchange step.
/// </summary>
/// <remarks>
/// Only <c>S256</c> is supported. The spec also defines <c>plain</c> but DNA refuses it because
/// MCP November 2025 mandates a cryptographic code-challenge method.
/// </remarks>
public sealed record PkceParameters(string CodeVerifier, string CodeChallenge, string CodeChallengeMethod = "S256")
{
    public const string SupportedCodeChallengeMethod = "S256";

    /// <summary>Minimum length per RFC 7636 §4.1.</summary>
    public const int MinVerifierLength = 43;

    /// <summary>Maximum length per RFC 7636 §4.1.</summary>
    public const int MaxVerifierLength = 128;
}
