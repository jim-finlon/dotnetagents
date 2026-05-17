using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace DotNetAgents.Contracts;

/// <summary>
/// Canonical reader for the per-service <c>dna.genetic.json</c> genetic-contract file. Walks up
/// from the application's ContentRootPath until it finds the contract or hits the filesystem root.
/// Replaces ~19 per-service copies that each implemented the same walk-and-parse with only the
/// service name differing in the error message.
///
/// Adoption: register via DI (<see cref="ServiceCollectionExtensions.AddGeneticContractReader"/>)
/// and inject <see cref="GeneticContractReader"/> where the raw JSON or contract path is needed.
/// Services that previously rolled their own may continue to do so; new services should take this
/// dependency directly.
/// </summary>
public sealed class GeneticContractReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _contractPath;
    private readonly string _serviceLabel;

    /// <summary>
    /// Initializes a new reader. <paramref name="serviceLabel"/> is used only in the
    /// <see cref="FileNotFoundException"/> message when the contract cannot be located and should
    /// be a human-readable service identifier (for example, "KnowledgeMemoryAgent").
    /// </summary>
    /// <param name="environment">The host environment — <see cref="IHostEnvironment.ContentRootPath"/>
    /// is the search origin.</param>
    /// <param name="serviceLabel">Human-readable service identifier for diagnostic messages.</param>
    public GeneticContractReader(IHostEnvironment environment, string serviceLabel)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceLabel);

        _serviceLabel = serviceLabel;
        _contractPath = FindContractPath(environment.ContentRootPath, serviceLabel);
    }

    /// <summary>
    /// Overload for tests and bespoke hosts that supply a content-root path directly without
    /// depending on <see cref="IHostEnvironment"/>.
    /// </summary>
    public GeneticContractReader(string contentRootPath, string serviceLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceLabel);

        _serviceLabel = serviceLabel;
        _contractPath = FindContractPath(contentRootPath, serviceLabel);
    }

    /// <summary>Absolute filesystem path of the located <c>dna.genetic.json</c>.</summary>
    public string ContractPath => _contractPath;

    /// <summary>Human-readable service label used in diagnostic messages.</summary>
    public string ServiceLabel => _serviceLabel;

    /// <summary>
    /// Reads the contract and returns it as a <see cref="JsonElement"/>. Use this when the service
    /// wants the raw contract shape — typical for endpoints that forward the contract verbatim.
    /// </summary>
    public async Task<JsonElement> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(_contractPath);
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the contract and returns both the parsed JSON and the absolute path. Convenience
    /// shape for endpoints that emit the contract alongside provenance metadata.
    /// </summary>
    public async Task<GeneticContractReadResult> ReadResponseAsync(CancellationToken cancellationToken = default)
    {
        var contract = await ReadAsync(cancellationToken).ConfigureAwait(false);
        return new GeneticContractReadResult(contract, _contractPath);
    }

    /// <summary>Synchronous read for hosts that already own the file content at startup.</summary>
    public JsonElement Read()
    {
        using var stream = File.OpenRead(_contractPath);
        using var doc = JsonDocument.Parse(stream);
        return doc.RootElement.Clone();
    }

    private static string FindContractPath(string startPath, string serviceLabel)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "dna.genetic.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Unable to locate dna.genetic.json for {serviceLabel}. Walked up from '{startPath}' to the filesystem root without finding the contract file.",
            "dna.genetic.json");
    }
}

/// <summary>Raw contract JSON paired with the absolute path of the file it was read from.</summary>
/// <param name="Contract">Parsed contract as a <see cref="JsonElement"/>.</param>
/// <param name="Source">Absolute path of the <c>dna.genetic.json</c> file.</param>
public readonly record struct GeneticContractReadResult(JsonElement Contract, string Source);
