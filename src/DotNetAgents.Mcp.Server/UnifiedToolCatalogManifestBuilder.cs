namespace DotNetAgents.Mcp.Server;

public static class UnifiedToolCatalogManifestBuilder
{
    public const int CurrentSchemaVersion = 1;

    public static UnifiedToolCatalogManifest Build(IEnumerable<UnifiedToolDescriptor> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);

        var ordered = tools
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UnifiedToolCatalogManifest(
            CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            ordered);
    }
}

public sealed record UnifiedToolCatalogValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    int ToolCount);

public static class UnifiedToolCatalogManifestValidator
{
    public static UnifiedToolCatalogValidationResult Validate(UnifiedToolCatalogManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var errors = new List<string>();
        if (manifest.SchemaVersion <= 0)
            errors.Add("SchemaVersion must be greater than 0.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tool in manifest.Tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Name))
                errors.Add("Tool name is required.");
            if (string.IsNullOrWhiteSpace(tool.Description))
                errors.Add($"Tool '{tool.Name}' has empty description.");
            if (!seen.Add(tool.Name))
                errors.Add($"Duplicate tool name '{tool.Name}'.");
            if (tool.LifecycleStatus == UnifiedToolLifecycleStatus.Unknown)
                errors.Add($"Tool '{tool.Name}' has unknown lifecycle status.");
            if (string.IsNullOrWhiteSpace(tool.Owner))
                errors.Add($"Tool '{tool.Name}' has no owner.");
        }

        return new UnifiedToolCatalogValidationResult(errors.Count == 0, errors, manifest.Tools.Count);
    }
}
