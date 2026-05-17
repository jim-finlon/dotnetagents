using System.Security.Cryptography;
using System.Text;

namespace DotNetAgents.Knowledge.Helpers;

/// <summary>
/// Helper class for calculating content hashes for knowledge items.
/// </summary>
public static class ContentHashHelper
{
    /// <summary>
    /// Calculates content hash for a knowledge item (title + description first 500 chars).
    /// </summary>
    /// <param name="title">The knowledge item title.</param>
    /// <param name="description">The knowledge item description.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    public static string CalculateContentHash(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or whitespace.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or whitespace.", nameof(description));

        var hashContent = $"{title}\n{description.Substring(0, Math.Min(500, description.Length))}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashContent));
        return Convert.ToHexString(hashBytes);
    }
}
