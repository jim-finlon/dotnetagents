# DotNetAgents

DotNetAgents is a .NET 10 platform for building production agents that can plan,
act, observe, improve, and operate under governance.

Most agent frameworks help you call a model. DotNetAgents is aimed at the next
step: systems of agents that can run real workflows, expose tools safely, learn
from execution evidence, route work by capability, and keep enough traceability
that teams can trust them in long-running software and business processes.

It is designed for developers who want agent systems to feel like serious
application infrastructure instead of one-off prompt scripts.

## Why DotNetAgents Exists

Agent software is moving from demos to operations. That changes the engineering
requirements.

You need more than a chat loop:

- agents with durable identity, capability metadata, and runtime contracts
- tools that can be exposed through MCP, A2A, HTTP, or in-process APIs
- workflows that can pause, resume, fan out, and be inspected
- model routing, structured output, memory, and observability that fit .NET
- governance hooks for approval, safety, release policy, and audit trails
- patterns for self-improving systems that capture evidence and feed it back
  into better agents, tools, prompts, workflows, and tests

DotNetAgents brings those pieces into a native .NET package train so teams can
build agentic applications with the same discipline they expect from the rest of
their platform.

## What You Can Build

DotNetAgents is useful when you are building:

- autonomous or semi-autonomous task agents
- agent-backed business workflows
- MCP servers and tool surfaces
- A2A-compatible agent endpoints and clients
- agent orchestration APIs for existing .NET applications
- state-machine, behavior-tree, supervisor, swarm, and worker-pool agents
- retrieval, document, multimodal, voice, and structured-output pipelines
- governed automation with approval, traceability, and observability
- systems that improve over time from test results, runtime evidence, and human
  feedback

The public core is intentionally broad. It gives you the primitives for the
agent runtime itself. Optional integrations live in companion repositories.

## Platform Pillars

### Native .NET First

DotNetAgents is written for modern C# and .NET 10. It fits dependency injection,
configuration, hosting, logging, OpenTelemetry, analyzers, and package-based
application delivery instead of wrapping a Python runtime.

### Agent Runtime Primitives

The core packages include contracts, runtime hosting, task orchestration,
subagents, tools, model routing, structured output, prompt runtime, skills,
memory, session persistence, channels, and workflow support.

### Multiple Agent Patterns

Use the pattern that fits the job:

- state machines for explicit lifecycle control
- behavior trees for tactical decision logic
- supervisors for task routing and delegation
- swarms and worker pools for parallel work
- hierarchical agents for organized teams
- workflows for repeatable business processes
- control loops for agents that observe, decide, act, and measure

### Tool and Protocol Surfaces

DotNetAgents includes public packages for MCP, MCP auth, MCP server hosting,
MCP stdio, A2A contracts, A2A clients, A2A servers, and conformance-oriented
tooling. The goal is to make agents usable by humans, applications, and other
agents without binding every caller to one transport.

### Governance and Observability

Production agents need accountability. DotNetAgents includes governance,
security, release policy, preview/confirm flows, observability, GenAI telemetry,
lane operations, and compliance predicate primitives so agent behavior can be
measured and controlled.

### Self-Improving Agent Systems

DotNetAgents is built around the idea that agent platforms should improve from
evidence. The public core includes primitives for runtime traces, structured
task knowledge, prompt/runtime separation, skills projection, lane profiles,
intent projection, and workflow feedback loops. Those pieces let applications
record what happened, evaluate outcomes, and feed changes back into the next
agent version.

## Repository Map

This repository is the public core package train.

| Area | Packages |
| --- | --- |
| Core runtime | `DotNetAgents.Abstractions`, `DotNetAgents.Core`, `DotNetAgents.Runtime`, `DotNetAgents.Hosting`, `DotNetAgents.Configuration`, `DotNetAgents.Contracts` |
| Agent patterns | `DotNetAgents.Agents.StateMachines`, `DotNetAgents.Agents.BehaviorTrees`, `DotNetAgents.Agents.Supervisor`, `DotNetAgents.Agents.Swarm`, `DotNetAgents.Agents.WorkerPool`, `DotNetAgents.Agents.Hierarchical` |
| Workflow and tasks | `DotNetAgents.Workflow`, `DotNetAgents.Workflow.Designer`, `DotNetAgents.Tasks`, `DotNetAgents.Agents.Tasks`, `DotNetAgents.Subagents` |
| Protocols and tools | `DotNetAgents.Mcp`, `DotNetAgents.Mcp.Server`, `DotNetAgents.Mcp.Auth`, `DotNetAgents.Mcp.Stdio`, `DotNetAgents.A2A`, `DotNetAgents.A2A.Client`, `DotNetAgents.A2A.Server`, `DotNetAgents.Tools.*` |
| Intelligence layer | `DotNetAgents.ModelRouting`, `DotNetAgents.PromptRuntime`, `DotNetAgents.StructuredOutput`, `DotNetAgents.AgenticRAG`, `DotNetAgents.Knowledge`, `DotNetAgents.CognitiveMesh` |
| Memory and sessions | `DotNetAgents.Memory.Advanced`, `DotNetAgents.Memory.Profile`, `DotNetAgents.SessionPersistence` |
| Governance and operations | `DotNetAgents.Governance`, `DotNetAgents.Security`, `DotNetAgents.ReleasePolicy`, `DotNetAgents.PreviewConfirm`, `DotNetAgents.LaneOps`, `DotNetAgents.Compliance.Predicates` |
| Observability | `DotNetAgents.Observability`, `DotNetAgents.Observability.AspNetCore`, `DotNetAgents.Observability.GenAi` |
| Developer surfaces | `DotNetAgents.CLI`, `DotNetAgents.REPL`, `DotNetAgents.Analyzers`, `DotNetAgents.AgentFramework` |
| Multimodal and voice | `DotNetAgents.MultiModal`, `DotNetAgents.Voice.*`, `DotNetAgents.Documents` |

Companion repositories:

- `dotnetagents-plugins` - optional adapters for storage, messaging, vector
  stores, UI, browser/computer-use, database tooling, media generation, and
  interoperability.
- `dotnetagents-examples` - runnable public examples and starter applications.

## Quick Start

The public package train is currently `1.0.0-preview.1` and targets .NET 10.

Create a .NET project:

```bash
dotnet new console -n MyAgentApp
cd MyAgentApp
dotnet add package DotNetAgents.Core --version 1.0.0-preview.1
dotnet add package DotNetAgents.Hosting --version 1.0.0-preview.1
dotnet add package DotNetAgents.Mcp.Server --version 1.0.0-preview.1
```

Then choose the pattern you need:

- start with `DotNetAgents.AgentFramework` for a simple application shell
- use `DotNetAgents.Agents.StateMachines` for explicit lifecycle control
- use `DotNetAgents.Agents.BehaviorTrees` for tactical decision logic
- use `DotNetAgents.Workflow` for repeatable multi-step processes
- add `DotNetAgents.Mcp.Server` when you want to expose tools to MCP clients
- add `DotNetAgents.A2A.Server` when another agent needs to call your agent

For complete starter projects, use the companion examples repository.

## Documentation

The `/docs` tree is the practical guide for using the public platform:

- [Getting Started](docs/getting-started.md) shows the first application shape
  and safe tool patterns.
- [Core Concepts](docs/core-concepts.md) explains the why behind the runtime,
  workflow, tool, protocol, governance, and evidence model.
- [Package Map](docs/package-map.md) helps you choose packages by job.
- [MCP and A2A](docs/mcp-and-a2a.md) explains which protocol surface to expose.
- [Governance and Observability](docs/governance-and-observability.md) covers
  approval, policy, telemetry, and evidence.
- [Open Core and Premium Path](docs/open-core-and-premium.md) explains what is
  public, what is optional, and what belongs in commercial layers.

## Comparison

If you are deciding whether DotNetAgents belongs in your stack, read
[COMPARISON.md](COMPARISON.md). The short version:

- choose DotNetAgents when you want a .NET-native agent platform with runtime,
  workflow, governance, protocol, and observability primitives in one package
  family
- use lighter libraries or scripts when you only need a model call or a small
  prompt chain
- combine DotNetAgents with other frameworks when you already have a Python,
  JavaScript, or cloud-specific agent estate and need a strong .NET execution
  surface

## Open Core Boundary

This repository is Apache-2.0 public core. The public surface is intended to be
useful on its own: you can build agents, workflows, tool servers, and runtime
hosts without private code.

Commercial and private systems may build on top of the public core with hosted
services, managed operations, premium adapters, vertical applications, support,
and enterprise governance packs. Those are not required to use this repository,
and they are not included here.

The same public primitives are leveraged in our private agent repositories to
run governed delivery, evaluation, memory, and operations workflows at a larger
scale. Premium packages such as DNA Factory build on DotNetAgents with managed
labs, evaluation evidence, certification receipts, premium plugins, hosted
agent operations, and opinionated governance packs. The public core shows the
foundation; the premium layer is for teams that want the operating system
around it.

## Roadmap

DotNetAgents is being opened in stages so the public platform stays useful
while premium/private systems keep their commercial advantage.

Public roadmap highlights:

- stronger examples for agent workflows, MCP servers, A2A endpoints, memory,
  voice, multimodal pipelines, and governed task automation
- public-safe documentation for self-improving agent systems, including how to
  capture evidence without leaking private implementation details
- package hardening toward a stable 1.0 API surface
- more adapters in `dotnetagents-plugins` for storage, messaging, vector search,
  media, browser/computer-use, and enterprise integration points
- an upcoming gamified Arena release that lets builders compare agents,
  workflows, and strategies in a public-friendly challenge format

Premium/private roadmap highlights:

- DNA Factory managed delivery, release evidence, and agent-operations layers
- premium evaluation, lab, and Arena packs for governed agent improvement
- commercial governance, compliance, credential, audit, and certification packs
- advanced model-routing and optimization packages used by private agent teams
- vertical agent templates and integration packs for customers who want a
  supported production path

We will talk about those capabilities at the product level. We will not publish
the premium code, private datasets, proprietary scoring logic, or operational
runbooks in this public repository.

## Release Status

DotNetAgents is in preview while the public API, package train, docs, and
examples are being hardened. Expect useful capabilities now, with ongoing
changes before a stable 1.0 release.

## Contributing

Start with [CONTRIBUTING.md](CONTRIBUTING.md). Contributions should preserve the
public/private boundary, keep examples public-safe, and prefer small changes
with tests or runnable validation.

## Security

Report security issues using [SECURITY.md](SECURITY.md). Do not open public
issues containing secrets, credentials, private infrastructure details, or
exploitable vulnerability details.

## License

DotNetAgents public core is licensed under Apache-2.0. See [LICENSE](LICENSE)
and [NOTICE](NOTICE).
