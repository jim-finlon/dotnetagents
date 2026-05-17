using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DotNetAgents.Skills;

/// <summary>
/// DI registration helpers for the Skills system. Story d76e675b — Phase 2C agent-OS keystone primitive #1.
/// </summary>
public static class SkillsServiceCollectionExtensions
{
    /// <summary>
    /// Register the Skills system with file-system registry + keyword retrieval. Configure
    /// scan directories via <paramref name="scanDirectories"/> — typical defaults are
    /// <c>~/.dotnetagents/skills/</c> and a repo-relative <c>skills/</c>.
    /// </summary>
    public static IServiceCollection AddSkillsSystem(
        this IServiceCollection services,
        IEnumerable<string> scanDirectories)
    {
        ArgumentNullException.ThrowIfNull(scanDirectories);
        var dirs = scanDirectories.ToArray();

        services.TryAddSingleton<ISkillRegistry>(sp => new FileSystemSkillRegistry(
            dirs,
            sp.GetService<ILogger<FileSystemSkillRegistry>>()));
        services.TryAddSingleton<ISkillRetriever, KeywordSkillRetriever>();

        return services;
    }

    /// <summary>Register a custom <see cref="ISkillRetriever"/> implementation (e.g. embedding-based) via factory.</summary>
    public static IServiceCollection AddSkillRetriever(
        this IServiceCollection services,
        Func<IServiceProvider, ISkillRetriever> factory)
    {
        services.RemoveAll<ISkillRetriever>();
        services.AddSingleton<ISkillRetriever>(factory);
        return services;
    }
}
