using System.Security.Cryptography;
using System.Text;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Verifies a manifest body's <c>sha256:&lt;hex&gt;</c> matches an expected value. Used by
/// <see cref="SkillSecurityPipeline"/> to detect drift between the bytes a capability pack
/// scored and the bytes the projection orchestrator is about to ship — exactly the cross-check
/// SUC-08's contents[].checksum is designed to enable.
/// </summary>
internal sealed class ChecksumVerifier
{
    public bool Verify(string body, string expected)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);
        var actual = Compute(body);
        return string.Equals(actual, expected, StringComparison.Ordinal);
    }

    public string Compute(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var bytes = Encoding.UTF8.GetBytes(body);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder("sha256:", 7 + hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
