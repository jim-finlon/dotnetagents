// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;

namespace DotNetAgents.PreviewConfirm;

/// <summary>Generates high-entropy confirmation tokens (URL-safe base64) for preview/confirm flows.</summary>
public static class PreviewConfirmToken
{
    /// <param name="byteLength">Raw entropy length before encoding (default 32 bytes = 256 bits).</param>
    public static string Create(int byteLength = 32)
    {
        if (byteLength < 16)
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Use at least 16 bytes of entropy for confirmation tokens.");

        var bytes = new byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        return ToBase64Url(bytes);
    }

    internal static string ToBase64Url(ReadOnlySpan<byte> data)
    {
        var b64 = Convert.ToBase64String(data);
        return b64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
