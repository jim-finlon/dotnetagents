using DotNetAgents.Mcp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Mcp.Configuration;

/// <summary>
/// Shared registration helper for learning-projection targets so MCP hosts
/// don't repeat the same Program.cs setup. Story e76fc19d.
///
/// Reads from an IConfiguration section (default key "LearningProjection")
/// shaped like:
///   {
///     "Enabled": true,
///     "TimeoutMs": 1500,
///     "Targets": [
///       { "Kind": "KnowledgeMemory",          "Url": "https://knowledge-memory.local",        "ApiKeyRef": "creds://KnowledgeMemoryApi" },
///       { "Kind": "SessionPersistence","Url": "https://sessions.local"        },
///       { "Kind": "EvaluationSandbox",       "Url": "https://evaluationsandbox.local",     "ApiKey": "literal-key" },
///       { "Kind": "Custom", "Name": "my-target", "Url": "https://my.local" }
///     ]
///   }
///
/// Each target's API key resolves through whichever <see cref="IApiKeyResolver"/>
/// is registered. Hosts that integrate CredentialsAgent register a resolver that
/// reads "creds://Category/Name" refs; hosts without one fall back to literal
/// ApiKey values from config.
/// </summary>
public static class LearningProjectionBinder
{
    public const string DefaultSectionName = "LearningProjection";

    /// <summary>
    /// Bind from an IConfiguration section. Resolves API keys through whichever
    /// <see cref="IApiKeyResolver"/> is registered (or falls back to literal
    /// ApiKey values when none is present). Names are auto-assigned from Kind
    /// when omitted (KnowledgeMemory/SessionPersistence/EvaluationSandbox); Custom targets
    /// must supply Name explicitly.
    /// </summary>
    public static IServiceCollection AddLearningProjectionFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddHttpClient();
        services.TryAddSingleton<IApiKeyResolver, PassthroughApiKeyResolver>();
        services.AddSingleton(sp =>
        {
            var section = configuration.GetSection(sectionName);
            var bound = new LearningProjectionConfigSection();
            section.Bind(bound);

            var resolver = sp.GetRequiredService<IApiKeyResolver>();
            var options = new AgentLearningProjectionOptions
            {
                Enabled = bound.Enabled,
                TimeoutMs = bound.TimeoutMs,
                Targets = bound.Targets.Select(t => MaterializeTarget(t, resolver)).Where(t => t is not null).ToList()!,
            };
            return options;
        });
        services.TryAddSingleton<DotNetAgents.Mcp.Abstractions.IAgentLearningProjector, DotNetAgents.Mcp.Adapters.AgentLearningProjector>();
        return services;
    }

    /// <summary>Materialize a target spec into the existing <see cref="AgentLearningProjectionTarget"/> shape.</summary>
    public static AgentLearningProjectionTarget? MaterializeTarget(LearningProjectionTargetSpec spec, IApiKeyResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(resolver);

        var name = string.IsNullOrWhiteSpace(spec.Name)
            ? KindToName(spec.Kind) ?? throw new InvalidOperationException("Custom targets require an explicit Name.")
            : spec.Name;

        if (string.IsNullOrWhiteSpace(spec.Url))
            return null;

        var resolvedKey = !string.IsNullOrWhiteSpace(spec.ApiKeyRef)
            ? resolver.Resolve(spec.ApiKeyRef!)
            : spec.ApiKey;

        return new AgentLearningProjectionTarget
        {
            Name = name,
            Enabled = spec.Enabled,
            Url = spec.Url,
            ApiKeyHeader = string.IsNullOrWhiteSpace(spec.ApiKeyHeader) ? "X-Api-Key" : spec.ApiKeyHeader,
            ApiKey = resolvedKey,
        };
    }

    private static string? KindToName(LearningProjectionTargetKind kind) => kind switch
    {
        LearningProjectionTargetKind.KnowledgeMemory => "knowledge-memory",
        LearningProjectionTargetKind.SessionPersistence => "session-persistence",
        LearningProjectionTargetKind.EvaluationSandbox => "evaluation-sandbox",
        LearningProjectionTargetKind.Custom => null,
        _ => null,
    };
}

public enum LearningProjectionTargetKind { Custom = 0, KnowledgeMemory = 1, SessionPersistence = 2, EvaluationSandbox = 3 }

public sealed class LearningProjectionConfigSection
{
    public bool Enabled { get; set; } = true;
    public int TimeoutMs { get; set; } = 1500;
    public List<LearningProjectionTargetSpec> Targets { get; set; } = new();
}

public sealed class LearningProjectionTargetSpec
{
    public LearningProjectionTargetKind Kind { get; set; } = LearningProjectionTargetKind.Custom;
    public string? Name { get; set; }
    public bool Enabled { get; set; } = true;
    public string Url { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ApiKeyRef { get; set; }
    public string? ApiKeyHeader { get; set; }
}

/// <summary>
/// Resolves an API-key reference (e.g. "creds://Category/Name") into a literal
/// API key string. Story e76fc19d. Hosts that integrate CredentialsAgent
/// register an implementation that calls the agent; hosts without one rely on
/// the default <see cref="PassthroughApiKeyResolver"/> which only handles
/// literal values.
/// </summary>
public interface IApiKeyResolver
{
    /// <summary>Resolve the reference to a literal API key. Return null if the reference cannot be resolved.</summary>
    string? Resolve(string reference);
}

/// <summary>
/// Default resolver that returns the reference unchanged when it doesn't look
/// like a scheme-based ref ("foo://...") and null otherwise. Hosts replace
/// this with a CredentialsAgent-backed resolver to get full ref handling.
/// </summary>
public sealed class PassthroughApiKeyResolver : IApiKeyResolver
{
    public string? Resolve(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        // If the value looks like a scheme reference we cannot resolve, return null
        // — caller's resolver should have handled it.
        return reference.Contains("://", StringComparison.Ordinal) ? null : reference;
    }
}
