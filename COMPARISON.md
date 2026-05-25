# DotNetAgents Comparison Guide

This guide helps you decide when DotNetAgents is the right foundation for an
agent system and when a lighter framework is a better fit.

It is not a winner-take-all comparison. Agent platforms are young, and many
teams will combine multiple tools. DotNetAgents is strongest when the agent
runtime itself needs to be part of a governed .NET application platform.

## Summary

| Need | DotNetAgents fit |
| --- | --- |
| .NET-native agent runtime | Strong fit. DotNetAgents is built for modern C# and .NET hosting. |
| Quick model call or prompt chain | Often more than you need. A small SDK wrapper may be enough. |
| MCP/A2A/tool server surfaces | Strong fit. Protocol and tool primitives are part of the public core. |
| Long-running workflow agents | Strong fit. Workflow, state-machine, supervisor, and worker patterns are first-class. |
| Production governance | Strong fit. Governance, preview/confirm, release policy, observability, and security primitives are built in. |
| Python data-science agent notebooks | Usually not the first choice. Pair with Python tools when that ecosystem is the core requirement. |
| Existing cloud-only agent service | Complementary. DotNetAgents can provide the .NET runtime and integration layer around managed services. |

## DotNetAgents vs Prompt Scripts

Prompt scripts are excellent for experiments:

- one model call
- one task
- simple state
- low operational risk
- local or personal automation

DotNetAgents is for the point where scripts become software:

- multiple agents and tools
- resumable workflows
- structured outputs
- repeatable runtime hosting
- observability and audit trails
- governance before high-impact actions
- reusable skills, tasks, memory, and protocol surfaces

If a failed run is just an inconvenience, a script may be enough. If a failed run
needs a trace, a retry plan, a policy decision, and a test, DotNetAgents is the
more natural shape.

## DotNetAgents vs LangChain and LangGraph

LangChain and LangGraph have large ecosystems and are common choices for Python
and JavaScript agent development. They are often a good fit when your team is
already centered on those runtimes or when you need a large catalog of community
integrations immediately.

DotNetAgents takes a different route:

- C# and .NET are the primary runtime, not a wrapper around a Python service
- dependency injection, hosting, configuration, logging, and OpenTelemetry are
  natural parts of the application model
- agent patterns include state machines, behavior trees, supervisors, swarms,
  worker pools, workflows, and control loops
- MCP and A2A surfaces sit beside in-process runtime primitives
- governance, security, preview/confirm, and release-policy concepts are part
  of the package family rather than afterthoughts

Choose DotNetAgents when the agent system is part of a .NET product, enterprise
application, or long-running service. Choose LangChain or LangGraph when the
center of gravity is Python/JavaScript experimentation, notebook-driven RAG, or
their integration ecosystem.

## DotNetAgents vs Semantic Kernel and AutoGen-Style .NET Options

Semantic Kernel and AutoGen-style options are useful for model orchestration,
planners, plugins, and multi-agent conversations. They also benefit from strong
vendor ecosystems.

DotNetAgents is broader at the application-platform layer:

- runtime hosting and agent identity
- workflow and task orchestration
- tool surfaces and protocol servers
- governance, policy, and operational safety
- observability and lane-oriented delivery concepts
- skills projection, prompt runtime, structured output, and model routing
- multiple agent control patterns beyond conversation loops

Use DotNetAgents when you need a full agent platform in .NET. Use vendor or
cloud frameworks when you want tight integration with that vendor's model,
planner, or managed-service ecosystem. They can also coexist: DotNetAgents can
host or integrate components that call other model orchestration libraries.

## DotNetAgents vs Workflow Engines

Traditional workflow engines are excellent when the process is known in advance:

- approval workflows
- integration pipelines
- long-running business transactions
- durable timers and retries
- explicit state transitions

DotNetAgents is useful when workflows need agentic judgment inside the process:

- task decomposition
- tool selection
- model routing
- adaptive recovery
- evidence-driven improvement
- policy-gated automation
- human review before risky actions

You do not have to choose one model for everything. DotNetAgents includes
workflow primitives and can also sit next to established workflow engines where
those engines own the durable process boundary.

## DotNetAgents vs Managed Agent Services

Managed agent services can reduce infrastructure burden and may be the fastest
way to ship a narrow experience. They are attractive when a single cloud
provider already owns the runtime, data plane, and operational model.

DotNetAgents is attractive when you want:

- source-level control of the agent runtime
- portability across model providers and hosting environments
- public package-based extension points
- deeper integration with existing .NET systems
- local development and self-hosted operation
- explicit governance and auditability in your own codebase

Managed services can still be part of the architecture. DotNetAgents can act as
the .NET application layer that calls those services, wraps them in policy, and
connects them to local tools and workflows.

## Where DotNetAgents Is Intentionally Different

DotNetAgents is opinionated about production readiness:

- agents are software components, not just prompts
- tool calls should be explicit, observable, and policy-aware
- runtime behavior should produce evidence
- evidence should feed testing, review, and the next iteration
- self-improvement should happen through controlled software delivery, not
  uncontrolled mutation

That makes the framework larger than a minimal prompt-chain helper. The tradeoff
is intentional: DotNetAgents is built for teams that expect agent systems to
survive contact with real users, real operations, and real accountability.

## Open Core and Premium Path

The public DotNetAgents repositories provide the foundation: runtime contracts,
agent patterns, tool/protocol surfaces, workflow primitives, observability
hooks, and public examples. They are intended to be valuable without buying
anything.

The premium and private path builds on that foundation. The same primitives are
used in private agent repositories to run governed delivery, evaluation,
memory, and operations workflows. Premium packages such as DNA Factory add the
managed operating layer: lab packs, evidence receipts, certification flows,
advanced routing, enterprise governance, and hosted agent operations.

The public roadmap also includes a gamified Arena release for builders who want
to compare agents and strategies in a challenge format. Public Arena material
will explain the product experience and safe extension points; proprietary
evaluation packs, scoring details, and operating procedures remain private.

## Decision Checklist

Choose DotNetAgents when most of these are true:

- your application is primarily .NET or C#
- agents need to expose or consume tools through stable contracts
- workflows need traceability, retries, or human approval
- model calls need structured output and routing
- observability and policy are requirements, not nice-to-haves
- you expect the system to improve through tests, feedback, traces, and release
  discipline

Choose a smaller tool when most of these are true:

- you only need a model call
- the agent is a short-lived prototype
- the runtime is already Python or JavaScript
- governance and observability are not yet important
- your integration needs are better served by another ecosystem today

## Bottom Line

DotNetAgents is for builders who want agent systems to become durable software:
composable, observable, governed, and capable of improving over time. If that is
the shape of your product, it gives you a native .NET foundation instead of a
pile of scripts around an API call.
