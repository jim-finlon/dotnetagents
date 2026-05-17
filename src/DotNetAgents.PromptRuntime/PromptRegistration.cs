namespace DotNetAgents.PromptRuntime;

/// <summary>
/// Declaration a host service registers at startup so the runtime knows which prompt keys it may
/// resolve and what to serve when PromptSpecialist is unreachable. Also feeds metadata
/// (task family, risk class, expected output schema) downstream so the prompt library can track
/// service/task ownership across the fleet.
/// </summary>
/// <param name="Key">Durable prompt key (domain.agent.purpose).</param>
/// <param name="Service">Owning DNA service name (e.g. workflow_service).</param>
/// <param name="TaskFamily">Task family (e.g. voice-note-classify, release-note-summarize).</param>
/// <param name="RiskClass">Risk class. Gates on the PromptSpecialist side check this.</param>
/// <param name="ExpectedOutputSchema">Short hint about the output shape (e.g. "json-strict", "markdown-bullets").</param>
/// <param name="FallbackBody">Compile-time prompt body used when PromptSpecialist is unreachable.</param>
/// <param name="FallbackVariables">Variable defaults used when the caller omits values on fallback.</param>
/// <param name="InstructionArtifactRef">Stable PromptSpecialist instruction artifact reference when this prompt maps to a curated artifact.</param>
/// <param name="ChainContractRefs">Chain contracts that consume this prompt artifact.</param>
/// <param name="SkillRefs">Skill references that consume or depend on this prompt artifact.</param>
public sealed record PromptRegistration(
    string Key,
    string Service,
    string TaskFamily,
    PromptRiskClass RiskClass,
    string ExpectedOutputSchema,
    string FallbackBody,
    IReadOnlyDictionary<string, string>? FallbackVariables = null,
    string? InstructionArtifactRef = null,
    IReadOnlyList<string>? ChainContractRefs = null,
    IReadOnlyList<string>? SkillRefs = null);

public enum PromptRiskClass
{
    Low = 0,
    Medium = 1,
    High = 2,
}
