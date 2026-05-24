// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Core.PublicExamples;

public static class PublicExampleResultEnvelopeContract
{
    public const string SchemaVersion = "dna.public-example.result.v1";
}

public sealed record PublicExampleResultEnvelope(
    string SchemaVersion,
    string ExampleId,
    string ExampleVersion,
    string RunId,
    DateTimeOffset TimestampUtc,
    string InputSummaryHash,
    IReadOnlyList<PublicExampleOutputArtifactRef> OutputArtifactRefs,
    PublicExampleValidationSummary LocalValidation,
    IReadOnlyDictionary<string, decimal>? SelfReportedMetrics = null)
{
    public static PublicExampleResultEnvelope Create(
        string exampleId,
        string exampleVersion,
        string inputSummary,
        PublicExampleValidationSummary localValidation,
        IEnumerable<PublicExampleOutputArtifactRef>? outputArtifactRefs = null,
        IReadOnlyDictionary<string, decimal>? selfReportedMetrics = null,
        string? runId = null,
        DateTimeOffset? timestampUtc = null)
    {
        return new PublicExampleResultEnvelope(
            PublicExampleResultEnvelopeContract.SchemaVersion,
            exampleId,
            exampleVersion,
            runId ?? Guid.NewGuid().ToString("N"),
            timestampUtc ?? DateTimeOffset.UtcNow,
            PublicExampleResultEnvelopeJson.ComputeInputSummaryHash(inputSummary),
            outputArtifactRefs?.ToArray() ?? [],
            localValidation,
            selfReportedMetrics);
    }
}

public sealed record PublicExampleOutputArtifactRef(
    string Kind,
    string Ref,
    string? MediaType = null,
    string? Sha256 = null);

public sealed record PublicExampleValidationSummary(
    string Status,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string>? Warnings = null)
{
    [JsonIgnore]
    public bool IsPassed => string.Equals(Status, "passed", StringComparison.OrdinalIgnoreCase);
}

public static class PublicExampleResultEnvelopeJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static string Serialize(PublicExampleResultEnvelope envelope)
    {
        Validate(envelope);
        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    public static PublicExampleResultEnvelope Deserialize(string json)
    {
        var envelope = JsonSerializer.Deserialize<PublicExampleResultEnvelope>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Public example result envelope JSON was empty.");

        Validate(envelope);
        return envelope;
    }

    public static async Task WriteAsync(
        string path,
        PublicExampleResultEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        Validate(envelope);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, Serialize(envelope), Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<PublicExampleResultEnvelope> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);
        return Deserialize(json);
    }

    public static string ComputeInputSummaryHash(string inputSummary)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(inputSummary));
        return "sha256:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static void Validate(PublicExampleResultEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        Require(PublicExampleResultEnvelopeContract.SchemaVersion, envelope.SchemaVersion, nameof(envelope.SchemaVersion));
        RequireNonEmpty(envelope.ExampleId, nameof(envelope.ExampleId));
        RequireNonEmpty(envelope.ExampleVersion, nameof(envelope.ExampleVersion));
        RequireNonEmpty(envelope.RunId, nameof(envelope.RunId));
        RequireNonEmpty(envelope.InputSummaryHash, nameof(envelope.InputSummaryHash));

        if (!envelope.InputSummaryHash.StartsWith("sha256:", StringComparison.Ordinal) ||
            envelope.InputSummaryHash.Length != "sha256:".Length + 64)
        {
            throw new InvalidOperationException("InputSummaryHash must be a sha256-prefixed hex digest.");
        }

        ArgumentNullException.ThrowIfNull(envelope.OutputArtifactRefs);
        foreach (var artifact in envelope.OutputArtifactRefs)
        {
            RequireNonEmpty(artifact.Kind, nameof(artifact.Kind));
            RequireNonEmpty(artifact.Ref, nameof(artifact.Ref));
        }

        ArgumentNullException.ThrowIfNull(envelope.LocalValidation);
        RequireNonEmpty(envelope.LocalValidation.Status, nameof(envelope.LocalValidation.Status));
        ArgumentNullException.ThrowIfNull(envelope.LocalValidation.Checks);
        if (envelope.LocalValidation.Checks.Count == 0)
        {
            throw new InvalidOperationException("LocalValidation.Checks must contain at least one check.");
        }

        foreach (var check in envelope.LocalValidation.Checks)
        {
            RequireNonEmpty(check, nameof(envelope.LocalValidation.Checks));
        }
    }

    private static void Require(string expected, string actual, string fieldName)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{fieldName} must be '{expected}'.");
        }
    }

    private static void RequireNonEmpty(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }
    }
}
