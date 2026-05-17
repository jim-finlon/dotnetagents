namespace DotNetAgents.Agents.LaneProfile;

public static class LaneProfileReferenceRegistry
{
    public static LanePolicyRegistry CreateDefault()
    {
        var coreGovernance = new LaneGovernancePack(
            "core4-session-persistence",
            "Core 4 and session persistence gate",
            NonWaivable: true,
            [
                "AGENTS.md#Core 4 + Session Persistence execution gate",
                ".cursor/rules/dna-core4-session-persistence-gate.mdc",
                ".cursor/rules/dna-cursor-mcp.mdc"
            ]);

        var worktreeGovernance = new LaneGovernancePack(
            "worktree-discipline",
            "DNA worktree lane discipline",
            NonWaivable: true,
            [
                "AGENTS.md#Worktree and lane operations",
                "docs/DNA-WORKTREE-COLLABORATION-RUNBOOK.md",
                "AgentProjects/WorkflowServices/docs/AUTONOMOUS-SUPERVISOR-GUIDANCE.md"
            ]);

        var credentialGovernance = new LaneGovernancePack(
            "credential-custody",
            "CredentialsAgent-first secret custody",
            NonWaivable: true,
            [
                "AGENTS.md#CredentialsAgent-first auth recovery",
                "docs/onboarding/credentials/AGENT-IDENTITY-CARDS.md"
            ]);

        var frontendPack = new LaneGovernancePack(
            "frontend-experience",
            "Frontend implementation guidance",
            NonWaivable: false,
            [
                "AGENTS.md#Frontend guidance",
                "docs/architecture/AGENT-INTENT-AND-DIRECTIVE-ROUTING.md"
            ]);

        var privilegedPack = new LaneGovernancePack(
            "privileged-lab-guardrails",
            "Privileged lab and infrastructure guardrails",
            NonWaivable: true,
            [
                "AGENTS.md#Tyr deploy + infrastructure",
                "docs/autonomous-mode/PILOT-SCOPE.md",
                "docs/runbooks/autonomous-lane/MODE-APPLICABILITY-MATRIX.md"
            ]);

        var skillBundles = new[]
        {
            new LaneSkillBundle("ui-frontend", "UI/frontend skills", ["frontend-design-guidance", "playwright-screenshot-validation"]),
            new LaneSkillBundle("dotnet-api", ".NET API skills", ["dotnet-validation", "mcp-contract-validation"]),
            new LaneSkillBundle("python-tooling", "Python/tooling skills", ["python-cli-validation", "text-encoding-hygiene"]),
            new LaneSkillBundle("docs-only", "Documentation skills", ["markdown-link-validation", "source-of-truth-routing"]),
            new LaneSkillBundle("privileged-lab", "Privileged lab skills", ["preview-confirm-audit", "credential-custody", "deployment-evidence"]),
            new LaneSkillBundle("local-llm-runner", "Local/open-source model runner skills", ["model-routing", "offline-runner-telemetry"])
        };

        return new LanePolicyRegistry(
            [coreGovernance, worktreeGovernance, credentialGovernance, frontendPack, privilegedPack],
            skillBundles,
            [
                CreateProfile(
                    "ui-only",
                    "UI only",
                    LaneCapabilityTier.TierA,
                    [LaneWorkClass.UiFrontend],
                    optionalPacks: ["frontend-experience"],
                    skillBundles: ["ui-frontend"],
                    mcp: [Core4("planning_tools"), Core4("ai-session-persistence")],
                    env: []),
                CreateProfile(
                    "dotnet-api",
                    ".NET API",
                    LaneCapabilityTier.TierA,
                    [LaneWorkClass.DotNetApi],
                    optionalPacks: [],
                    skillBundles: ["dotnet-api"],
                    mcp: [Core4("planning_tools"), Core4("ai-session-persistence"), Core4("credentials")],
                    env: [new("WORKFLOW_SERVICE_API_KEY", "credential-ref/workflow/api-key")]),
                CreateProfile(
                    "python-tool",
                    "Python tooling",
                    LaneCapabilityTier.TierA,
                    [LaneWorkClass.PythonTooling],
                    optionalPacks: [],
                    skillBundles: ["python-tooling"],
                    mcp: [Core4("planning_tools"), Core4("ai-session-persistence")],
                    env: []),
                CreateProfile(
                    "docs-only",
                    "Docs only",
                    LaneCapabilityTier.TierB,
                    [LaneWorkClass.DocsOnly],
                    optionalPacks: [],
                    skillBundles: ["docs-only"],
                    mcp: [Core4("planning_tools"), Core4("ai-session-persistence")],
                    env: []),
                CreateProfile(
                    "privileged-lab",
                    "Privileged lab",
                    LaneCapabilityTier.Privileged,
                    [LaneWorkClass.PrivilegedLab],
                    optionalPacks: ["privileged-lab-guardrails"],
                    skillBundles: ["privileged-lab"],
                    mcp: [Core4("planning_tools"), Core4("ai-session-persistence"), Core4("credentials"), Core4("knowledge-memory")],
                    env:
                    [
                        new("CREDENTIAL_STORE_API_KEY", "credential-ref/credential-store/admin-api-key"),
                        new("WORKFLOW_SERVICE_API_KEY", "credential-ref/workflow/api-key")
                    ],
                    requiresGate: true),
                CreateProfile(
                    "local-llm-runner",
                    "Local/open-source LLM runner",
                    LaneCapabilityTier.TierA,
                    [LaneWorkClass.LocalOpenSourceLlmRunner],
                    optionalPacks: [],
                    skillBundles: ["local-llm-runner"],
                    mcp: [Core4("planning_tools"), Core4("ai-session-persistence")],
                    env: [new("LOCAL_LLM_ENDPOINT", "credential-ref/model-routing/local-runner-endpoint", Required: false)])
            ]);
    }

    private static LaneProfileDefinition CreateProfile(
        string id,
        string displayName,
        LaneCapabilityTier tier,
        IReadOnlyList<LaneWorkClass> workClasses,
        IReadOnlyList<string> optionalPacks,
        IReadOnlyList<string> skillBundles,
        IReadOnlyList<LaneMcpDependency> mcp,
        IReadOnlyList<LaneEnvironmentReference> env,
        bool requiresGate = false)
        => new(
            id,
            displayName,
            tier,
            workClasses,
            RequiredGovernancePackIds: ["core4-session-persistence", "worktree-discipline", "credential-custody"],
            OptionalSpecialistPackIds: optionalPacks,
            SkillBundleIds: skillBundles,
            McpDependencies: mcp,
            EnvironmentReferences: env,
            RestartRequirements:
            [
                new("persist-session-handoff", "Persist Session Persistence and SDLC handoff before restart or re-specialization.", RequiresProcessRestart: false),
                new("reload-tool-surface", "Restart the host/client when projected rules, skills, MCP endpoints, or model-runner config change.", RequiresProcessRestart: true)
            ],
            ValidationChecks:
            [
                new("non-waivable-governance", "All non-waivable governance packs must be present."),
                new("skill-bundles", "Every declared skill bundle must resolve to a manifest reference."),
                new("mcp-dependencies", "Required MCP/Core service dependencies must be present in the receipt."),
                new("env-vars-by-reference", "Environment variables must render credential references only, never secret values.")
            ],
            RequiresOperatorGate: requiresGate);

    private static LaneMcpDependency Core4(string serviceId)
        => new(serviceId, Required: true, HealthRef: $"mcp://{serviceId}/health");
}
