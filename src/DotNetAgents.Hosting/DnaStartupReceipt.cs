namespace DotNetAgents.Hosting;

/// <summary>
/// Redaction-safe startup receipt published when <see cref="DnaServiceHostOptions.EnableStartupReceipt"/> is true.
/// Designed to be persisted to logs, dashboards, and audit packs without secret material.
/// </summary>
public sealed record DnaStartupReceipt(
    string ServiceName,
    string DisplayName,
    string Version,
    string DeploymentRing,
    DateTimeOffset StartedAtUtc,
    string EnvironmentName,
    bool ProblemDetailsEnabled,
    string HealthLivePath,
    string HealthReadyPath,
    string HealthAggregatePath,
    string OperatorRunbookPath);

/// <summary>In-memory store of the most recent startup receipt for this process.</summary>
public interface IDnaStartupReceiptStore
{
    /// <summary>Records the receipt; replaces any previous receipt.</summary>
    void Record(DnaStartupReceipt receipt);

    /// <summary>Returns the most recent receipt, or null when one has not been recorded yet.</summary>
    DnaStartupReceipt? Current { get; }
}

internal sealed class DnaStartupReceiptStore : IDnaStartupReceiptStore
{
    private DnaStartupReceipt? _current;

    public DnaStartupReceipt? Current => _current;

    public void Record(DnaStartupReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        _current = receipt;
    }
}
