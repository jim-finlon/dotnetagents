using Microsoft.Extensions.Logging;

namespace DotNetAgents.Skills;

/// <summary>
/// File-system-backed <see cref="ISkillRegistry"/>: scans configured directories for skill
/// folders (each folder must contain a SKILL.md), parses descriptors, caches in memory.
/// Reload is explicit via <see cref="ReloadAsync"/>.
/// </summary>
public sealed class FileSystemSkillRegistry : ISkillRegistry
{
    private readonly IReadOnlyList<string> _scanDirectories;
    private readonly ILogger<FileSystemSkillRegistry>? _logger;
    private volatile IReadOnlyList<SkillDescriptor> _skills;
    private volatile Dictionary<string, SkillDescriptor> _byId;

    public FileSystemSkillRegistry(
        IEnumerable<string> scanDirectories,
        ILogger<FileSystemSkillRegistry>? logger = null,
        bool loadOnConstruct = true)
    {
        _scanDirectories = scanDirectories?.ToArray() ?? Array.Empty<string>();
        _logger = logger;
        _skills = Array.Empty<SkillDescriptor>();
        _byId = new Dictionary<string, SkillDescriptor>(StringComparer.OrdinalIgnoreCase);

        if (loadOnConstruct)
        {
            try { ReloadAsync().GetAwaiter().GetResult(); }
            catch (Exception ex) { _logger?.LogWarning(ex, "Initial skill registry load failed; registry starts empty."); }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ScanDirectories => _scanDirectories;

    /// <inheritdoc />
    public IReadOnlyList<SkillDescriptor> All() => _skills;

    /// <inheritdoc />
    public SkillDescriptor? GetById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _byId.TryGetValue(id, out var d) ? d : null;
    }

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        var loaded = new List<SkillDescriptor>();

        foreach (var dir in _scanDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(dir))
            {
                _logger?.LogDebug("Scan directory does not exist: {Dir}", dir);
                continue;
            }

            foreach (var skillFolder in Directory.EnumerateDirectories(dir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var skillMdPath = Path.Combine(skillFolder, "SKILL.md");
                if (!File.Exists(skillMdPath)) continue;

                try
                {
                    var content = await File.ReadAllTextAsync(skillMdPath, cancellationToken).ConfigureAwait(false);
                    var parsed = SkillMarkdownParser.Parse(content);
                    if (string.IsNullOrEmpty(parsed.Name) || string.IsNullOrEmpty(parsed.Description))
                    {
                        _logger?.LogWarning("Skipping skill folder {Folder}: missing required name or description in frontmatter.", skillFolder);
                        continue;
                    }

                    var resourceFiles = Directory
                        .EnumerateFiles(skillFolder, "*", SearchOption.AllDirectories)
                        .Where(f => !string.Equals(Path.GetFileName(f), "SKILL.md", StringComparison.OrdinalIgnoreCase))
                        .Select(f => Path.GetRelativePath(skillFolder, f))
                        .ToArray();

                    var id = Path.GetFileName(skillFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    loaded.Add(new SkillDescriptor(
                        Id: id,
                        Name: parsed.Name,
                        Description: parsed.Description,
                        Version: parsed.Version,
                        Instructions: parsed.Body,
                        ResourceFiles: resourceFiles,
                        Dependencies: parsed.Dependencies,
                        Scripts: parsed.Scripts,
                        FolderPath: skillFolder));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to load skill from {Folder}.", skillFolder);
                }
            }
        }

        var ordered = loaded.OrderBy(s => s.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        var byId = ordered.ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);

        _skills = ordered;
        _byId = byId;

        _logger?.LogInformation("Skill registry loaded {Count} skills from {DirCount} directories.",
            ordered.Length, _scanDirectories.Count);
    }
}
