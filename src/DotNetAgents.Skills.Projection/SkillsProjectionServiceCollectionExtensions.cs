// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Skills.Projection;

/// <summary>
/// DI registration for the SUC projection framework. Story aff01407 (P1) introduced the
/// foundation + the eight default-constructible projectors. Story b5a0cff6 (P2) extended
/// this to register all eleven named projector implementations via
/// <see cref="SkillsProjectionOptions"/>.
/// </summary>
public static class SkillsProjectionServiceCollectionExtensions
{
    /// <summary>
    /// Register the projection framework with default options: the eight default-constructible
    /// projectors, one <see cref="LocalLlmToolProjector"/> (OpenAI tool JSON kind), one
    /// <see cref="OpenAiToolProjector"/> (tool JSON kind), zero
    /// <see cref="AgentSkillsIoStandardProjector"/>s (callers add targets via the
    /// <see cref="SkillsProjectionOptions"/> overload), and the production atomic-file applier.
    /// </summary>
    /// <param name="services">DI service collection.</param>
    /// <param name="useDryRunApplier">
    /// When <c>true</c>, registers <see cref="DryRunProjectionApplier"/> as the
    /// <see cref="IProjectionApplier"/> singleton instead of the production atomic-file
    /// applier. Used by tests + the SUC-17 panel-load metric path (story 9bd8f796 Slice B)
    /// that needs to count "would-be vendor projections" without writing to disk.
    /// </param>
    public static IServiceCollection AddSkillsProjection(
        this IServiceCollection services,
        bool useDryRunApplier = false)
        => AddSkillsProjection(services, configure: null, useDryRunApplier);

    /// <summary>
    /// Register the projection framework with caller-supplied options. Each entry in
    /// <see cref="SkillsProjectionOptions.AgentSkillsIoTargets"/> becomes a separate
    /// <see cref="AgentSkillsIoStandardProjector"/> registration; each kind in
    /// <see cref="SkillsProjectionOptions.LocalLlmKinds"/> and
    /// <see cref="SkillsProjectionOptions.OpenAiToolKinds"/> becomes a separate projector
    /// registration of the corresponding type.
    /// </summary>
    public static IServiceCollection AddSkillsProjection(
        this IServiceCollection services,
        Action<SkillsProjectionOptions>? configure,
        bool useDryRunApplier = false)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SkillsProjectionOptions();
        configure?.Invoke(options);

        // Eight default-constructible projectors (P1).
        services.AddSingleton<ISkillProjector, AgentsMdProjector>();
        services.AddSingleton<ISkillProjector, ClaudeCodeProjector>();
        services.AddSingleton<ISkillProjector, CodexOpenAiYamlProjector>();
        services.AddSingleton<ISkillProjector, CodexSkillProjector>();
        services.AddSingleton<ISkillProjector, CopilotInstructionsProjector>();
        services.AddSingleton<ISkillProjector, CopilotSkillProjector>();
        services.AddSingleton<ISkillProjector, CursorRuleProjector>();
        services.AddSingleton<ISkillProjector, CursorSkillProjector>();

        // P2: parameterized projectors — one registration per configured variant.
        foreach (var kind in options.LocalLlmKinds)
        {
            var family = options.LocalLlmTemplateFamily;
            services.AddSingleton<ISkillProjector>(_ => new LocalLlmToolProjector(kind, family));
        }
        foreach (var kind in options.OpenAiToolKinds)
        {
            services.AddSingleton<ISkillProjector>(_ => new OpenAiToolProjector(kind));
        }
        foreach (var target in options.AgentSkillsIoTargets)
        {
            services.AddSingleton<ISkillProjector>(_ => new AgentSkillsIoStandardProjector(target));
        }

        if (useDryRunApplier)
        {
            services.RemoveAll<IProjectionApplier>();
            services.AddSingleton<DryRunProjectionApplier>();
            services.AddSingleton<IProjectionApplier>(sp => sp.GetRequiredService<DryRunProjectionApplier>());
        }
        else
        {
            services.TryAddSingleton<IProjectionApplier, AtomicFileProjectionApplier>();
        }

        return services;
    }
}

/// <summary>
/// Caller-supplied options for parameterized projectors registered by
/// <see cref="SkillsProjectionServiceCollectionExtensions.AddSkillsProjection(IServiceCollection, Action{SkillsProjectionOptions}?, bool)"/>.
/// Story b5a0cff6 (SUC Projection Framework P2).
/// </summary>
public sealed class SkillsProjectionOptions
{
    /// <summary>
    /// One <see cref="AgentSkillsIoStandardProjector"/> is registered per target. Default: empty
    /// (callers can populate from <see cref="AgentSkillsIoStandardProjector.CreateAllFromConfig"/>
    /// or in-code).
    /// </summary>
    public IList<AgentSkillsIoTarget> AgentSkillsIoTargets { get; } = new List<AgentSkillsIoTarget>();

    /// <summary>
    /// One <see cref="LocalLlmToolProjector"/> registered per kind. Default: just
    /// <see cref="LocalLlmProjectionKind.OpenAiToolJson"/> — the most generally useful surface.
    /// </summary>
    public IList<LocalLlmProjectionKind> LocalLlmKinds { get; } = new List<LocalLlmProjectionKind>
    {
        LocalLlmProjectionKind.OpenAiToolJson,
    };

    /// <summary>
    /// Template family threaded into every <see cref="LocalLlmToolProjector"/> registration.
    /// Defaults to <see cref="LocalLlmTemplateFamily.Hermes"/> per the existing class default.
    /// </summary>
    public LocalLlmTemplateFamily LocalLlmTemplateFamily { get; set; } = LocalLlmTemplateFamily.Hermes;

    /// <summary>
    /// One <see cref="OpenAiToolProjector"/> registered per kind. Default: just
    /// <see cref="OpenAiToolProjectionKind.ToolJson"/>.
    /// </summary>
    public IList<OpenAiToolProjectionKind> OpenAiToolKinds { get; } = new List<OpenAiToolProjectionKind>
    {
        OpenAiToolProjectionKind.ToolJson,
    };
}
