using System.Text;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// Story aff01407 (SUC Projection Framework P1). Idempotent file writer used by the
/// projection applier path: writes the rendered <see cref="SkillProjection.Contents"/> to
/// a sibling <c>&lt;name&gt;.tmp</c> in the target directory, fsyncs, then renames into
/// place. A partial failure leaves the temp file behind (best-effort deletion on the
/// failure path) but never a half-written target.
/// </summary>
/// <remarks>
/// <para>Idempotency: if the destination already exists and its UTF-8 contents byte-equal the
/// proposed payload, the writer is a no-op (no temp file created, no rename). Callers see
/// <see cref="AtomicWriteOutcome.NoChange"/> so they can suppress audit churn.</para>
///
/// <para>Windows + WSL compatibility: <see cref="File.Move(string, string, bool)"/> with
/// <c>overwrite=true</c> is atomic on the same volume on both NTFS (via the underlying
/// MoveFileEx + MOVEFILE_REPLACE_EXISTING) and Linux (via rename(2)). Cross-volume moves are
/// rejected by callers — projection target paths are always repo-root-relative or actor-home-relative.</para>
/// </remarks>
public static class AtomicFileWriter
{
    /// <summary>
    /// Write <paramref name="contents"/> to <paramref name="targetFullPath"/> atomically.
    /// Creates the parent directory if missing. Idempotent: if the destination already
    /// matches byte-for-byte, returns <see cref="AtomicWriteOutcome.NoChange"/> without
    /// touching disk.
    /// </summary>
    public static async Task<AtomicWriteOutcome> WriteAsync(
        string targetFullPath,
        string contents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFullPath);
        ArgumentNullException.ThrowIfNull(contents);

        var directory = Path.GetDirectoryName(targetFullPath);
        if (string.IsNullOrEmpty(directory))
            throw new ArgumentException($"Target path '{targetFullPath}' has no directory component.", nameof(targetFullPath));

        Directory.CreateDirectory(directory);

        var payload = Encoding.UTF8.GetBytes(contents);
        if (File.Exists(targetFullPath))
        {
            var existing = await File.ReadAllBytesAsync(targetFullPath, cancellationToken).ConfigureAwait(false);
            if (BytesEqual(existing, payload))
                return AtomicWriteOutcome.NoChange;
        }

        var tempPath = targetFullPath + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllBytesAsync(tempPath, payload, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, targetFullPath, overwrite: true);
            return AtomicWriteOutcome.Written;
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch { /* best-effort temp cleanup */ }
            throw;
        }
    }

    private static bool BytesEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}

/// <summary>Outcome of an <see cref="AtomicFileWriter.WriteAsync(string, string, CancellationToken)"/> call.</summary>
public enum AtomicWriteOutcome
{
    /// <summary>The target was written or replaced.</summary>
    Written,

    /// <summary>The target already matched byte-for-byte; nothing was touched.</summary>
    NoChange,
}
