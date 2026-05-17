# DotNetAgents.Runtime

`DotNetAgents.Runtime` is the runtime and trajectory skeleton for Hermes/GEPA-inspired orchestration work. The first tranche is intentionally small: it records sessions, messages, model calls, tool invocations, context snapshots, and trajectory artifacts without requiring external model credentials.

Primary extension seams:

- **`AgentExecutor` vs `AgentRuntime`** — see [AGENT-EXECUTOR-VS-AGENT-RUNTIME.md](../docs/architecture/AGENT-EXECUTOR-VS-AGENT-RUNTIME.md). `AgentExecutor` (Core) runs multi-iteration ReAct; `AgentRuntime` runs one model turn plus a tool batch with session/trajectory persistence. `AgentToolLoop` is the shared single-pass `ITool` contract used by the runtime.
- `IAgentRuntime` runs one agent turn and emits a trajectory artifact.
- `IAgentSessionStore` owns durable session lineage for interactive, compressed, scheduled, child, and delegated sessions.
- `ITrajectoryRecorder` emits structured artifacts that later EvaluationSandbox, GEPA/quality score optimizers, and replay tooling can consume.
- `IAgentTurnModel` and `IToolsetResolver` keep provider routing and tool catalogs replaceable.
- `IDelegationBroker`, `IDelegationPolicy`, and `IDelegatedRunStore` provide a bounded delegated-agent primitive for summary-only subagent work without promoting every task into an SDLC worktree lane.
- `IExecutionEnvironmentProvider`, `ICommandExecutor`, `IArtifactCollector`, and `IEnvironmentCleanupPolicy` describe scoped execution leases with blast-radius, credential, network, persistence, approval, command-output, artifact, and cleanup receipt metadata.
- `AgentProgramDefinition` and `AgentCompiledVariant` describe authored agent programs and optimizer-produced variants without embedding optimizer execution, raw prompt text, or credentials.
- Placeholder seams identify later integration points for memory compaction, toolset catalog policy, scheduled jobs, gateway sessions, and EvaluationSandbox fixtures.

## Runtime Delegation

`InProcessDelegationBroker` starts a child `AgentRunMode.Delegated` session through `IAgentRuntime`, records parent-child lineage, and returns only a concise summary plus artifact and trajectory refs. It is for bounded research, summarization, verification, and parallel tool work inside an already-owned runtime session.

It is not SDLC story ownership, automated worker execution, deployment approval, or a substitute for workflow control plane completion. Default policy rejects recursive delegation, credential/secret tools, memory-write tools, destructive filesystem/shell tools, schedule/cron creation, and production deploy actions unless a later explicit policy story grants a bounded exception.

## Execution Environment Leases

`InMemoryExecutionEnvironmentProvider` is the v1 test provider for the execution lease contract. It creates scoped fake worktree leases, records actor/purpose/base commit/path/branch metadata, exposes safe command output and artifact references, and only cleans the exact path owned by the lease. It does not execute shell commands or delete files; real providers should adapt the same contract around DNA worktree scripts, Docker/container sandboxes, SSH hosts, k3s jobs, or cloud sandboxes while preserving path-scoped cleanup receipts and secret-free output references.

## Agent Program Contracts

`AgentProgramDefinition` is the source-of-truth authoring contract for a compound agent behavior. It can reference typed inputs and outputs, modules, Prompt Gene components, SkillCatalog entries, Toolset Catalog entries, retrieval and memory policies, behavior-tree/control-flow refs, evaluator refs, optimizer policy refs, and provenance. It is distinct from a raw prompt string, a Prompt Gene record, a skill package, a behavior tree, or a runtime session.

`AgentCompiledVariant` is the optimizer output contract. It records the source program id/version, compile strategy, frozen component refs, evaluator/evidence refs, budget summaries, and rollback target so EvaluationSandbox and promotion gates can compare variants without rewriting the source program definition. `AgentProgramDefinitionValidator` keeps the v1 contract fail-closed for required fields and rejects credential-like inline payload markers in refs.

Router map:

- Source contract and validation: `AgentProgramModels.cs`.
- Strategy/receipt contract that can compile source programs: `OptimizerStrategyModels.cs`.
- Tests: `DotNetAgents.Runtime.Tests/AgentProgramDefinitionTests.cs` and `OptimizerStrategyTests.cs`.
- Research basis: `docs/research/DSPY-TEXTGRAD-OPTIMIZER-LANDSCAPE-2026-05-15.md`.

This package does not copy Hermes, GEPA, or DSPy code. It provides DNA-native contracts that can absorb useful ideas from those systems behind stable .NET interfaces.
