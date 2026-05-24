// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetAgents.Agents.IntentProjector;

public sealed class IntentProjector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TimeProvider _timeProvider;

    public IntentProjector(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IntentProjectionReceipt Project(IntentDocument document, IntentProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(request);

        var consumer = document.Consumers.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, request.ConsumerId, StringComparison.OrdinalIgnoreCase))
            ?? throw new IntentProjectionException($"Unknown intent consumer '{request.ConsumerId}'.");

        if (!consumer.SupportedProjectionKinds.Contains(request.Kind))
        {
            throw new IntentProjectionException($"Consumer '{consumer.Id}' does not support projection kind '{request.Kind}'.");
        }

        var validation = Validate(document).ToArray();
        if (validation.Length > 0)
        {
            throw new IntentProjectionException($"Intent document '{document.Id}' is not projectable: {string.Join("; ", validation)}");
        }

        var blocks = SelectBlocks(document.Blocks, request, consumer).ToArray();
        var artifact = request.Kind switch
        {
            IntentProjectionKind.AgentsMarkdown => MarkdownArtifact("AGENTS.generated.md", document, consumer, blocks),
            IntentProjectionKind.RuleMarkdown => MarkdownArtifact("rules.generated.md", document, consumer, blocks),
            IntentProjectionKind.ModelPrompt => PromptArtifact("model-prompt.txt", document, consumer, blocks),
            IntentProjectionKind.ToolPrompt => PromptArtifact("tool-prompt.txt", document, consumer, blocks),
            IntentProjectionKind.ConfigJson => ConfigArtifact("intent.config.json", document, consumer, blocks),
            _ => throw new IntentProjectionException($"Projection kind '{request.Kind}' is not implemented.")
        };

        var artifacts = new[] { artifact };
        if (!string.IsNullOrWhiteSpace(request.TargetRoot))
        {
            Materialize(request.TargetRoot, artifacts);
        }

        return new IntentProjectionReceipt(
            document.Id,
            consumer.Id,
            request.Kind,
            _timeProvider.GetUtcNow(),
            artifacts,
            validation);
    }

    private static IEnumerable<string> Validate(IntentDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Id))
            yield return "document id is required";
        if (string.IsNullOrWhiteSpace(document.Title))
            yield return "document title is required";
        if (document.Blocks.Count == 0)
            yield return "at least one intent block is required";

        foreach (var duplicate in document.Blocks.GroupBy(block => block.Id, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            yield return $"duplicate block id '{duplicate.Key}'";

        foreach (var block in document.Blocks)
        {
            if (string.IsNullOrWhiteSpace(block.Id))
                yield return "block id is required";
            if (string.IsNullOrWhiteSpace(block.Title))
                yield return $"block '{block.Id}' title is required";
            if (block.Security == IntentSecurityClassification.SecretReferenceOnly && !string.IsNullOrWhiteSpace(block.Body))
                yield return $"block '{block.Id}' is secret-reference-only and must not inline body content";
        }
    }

    private static IEnumerable<IntentBlock> SelectBlocks(
        IEnumerable<IntentBlock> blocks,
        IntentProjectionRequest request,
        IntentConsumerProfile consumer)
    {
        var includeTags = request.IncludeTags is { Count: > 0 }
            ? new HashSet<string>(request.IncludeTags, StringComparer.OrdinalIgnoreCase)
            : null;

        return blocks
            .Where(block => request.IncludeReferenceBlocks || block.Role != IntentBlockRole.Reference)
            .Where(block => includeTags is null || (block.Tags ?? Array.Empty<string>()).Any(includeTags.Contains))
            .Where(block => !consumer.RequiresOfflineSafeOutput || block.Security != IntentSecurityClassification.Confidential)
            .OrderBy(block => block.Precedence)
            .ThenBy(block => block.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static IntentProjectionArtifact MarkdownArtifact(
        string fileName,
        IntentDocument document,
        IntentConsumerProfile consumer,
        IReadOnlyList<IntentBlock> blocks)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {document.Title}");
        builder.AppendLine();
        builder.AppendLine($"Intent id: `{document.Id}`");
        builder.AppendLine($"Version: `{document.Version}`");
        builder.AppendLine($"Consumer: `{consumer.Id}` ({consumer.Kind})");
        builder.AppendLine();
        builder.AppendLine(document.Summary);
        builder.AppendLine();

        foreach (var block in blocks)
        {
            builder.AppendLine($"## {block.Title}");
            builder.AppendLine();
            builder.AppendLine($"Role: `{block.Role}` | Scope: `{block.Scope}` | Precedence: `{block.Precedence}` | Security: `{block.Security}`");
            AppendRefs(builder, "Sources", block.SourceRefs);
            AppendRefs(builder, "Credential refs", block.CredentialRefs);
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(block.Body))
            {
                builder.AppendLine(block.Body);
                builder.AppendLine();
            }
        }

        return new IntentProjectionArtifact($".dna/intent/{fileName}", "text/markdown", builder.ToString());
    }

    private static IntentProjectionArtifact PromptArtifact(
        string fileName,
        IntentDocument document,
        IntentConsumerProfile consumer,
        IReadOnlyList<IntentBlock> blocks)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"INTENT: {document.Title}");
        builder.AppendLine($"CONSUMER: {consumer.DisplayName} [{consumer.Kind}]");
        builder.AppendLine("Honor role, scope, precedence, and security labels on every block.");
        builder.AppendLine();

        foreach (var block in blocks)
        {
            builder.AppendLine($"[{block.Role} | {block.Scope} | precedence={block.Precedence} | security={block.Security}] {block.Title}");
            if (block.CredentialRefs is { Count: > 0 })
                builder.AppendLine($"CredentialRefs: {string.Join(", ", block.CredentialRefs)}");
            if (!string.IsNullOrWhiteSpace(block.Body))
                builder.AppendLine(block.Body);
            builder.AppendLine();
        }

        return new IntentProjectionArtifact($".dna/intent/{fileName}", "text/plain", builder.ToString());
    }

    private static IntentProjectionArtifact ConfigArtifact(
        string fileName,
        IntentDocument document,
        IntentConsumerProfile consumer,
        IReadOnlyList<IntentBlock> blocks)
    {
        var payload = new
        {
            document.Id,
            document.Title,
            document.Version,
            consumer = new { consumer.Id, consumer.DisplayName, consumer.Kind },
            blocks = blocks.Select(block => new
            {
                block.Id,
                block.Title,
                block.Role,
                block.Scope,
                block.Precedence,
                block.Security,
                block.Tags,
                block.SourceRefs,
                block.CredentialRefs,
                block.Body
            })
        };
        return new IntentProjectionArtifact($".dna/intent/{fileName}", "application/json", JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static void AppendRefs(StringBuilder builder, string label, IReadOnlyList<string>? refs)
    {
        if (refs is not { Count: > 0 })
            return;

        builder.AppendLine($"{label}:");
        foreach (var item in refs)
            builder.AppendLine($"- `{item}`");
    }

    private static void Materialize(string targetRoot, IEnumerable<IntentProjectionArtifact> artifacts)
    {
        var root = Path.GetFullPath(targetRoot);
        foreach (var artifact in artifacts)
        {
            var path = Path.GetFullPath(Path.Combine(root, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            var safeRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase))
                throw new IntentProjectionException($"Artifact path escapes target root: {artifact.RelativePath}");

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, artifact.Content);
        }
    }
}
