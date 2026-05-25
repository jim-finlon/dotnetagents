# DotNetAgents Documentation

These docs are for developers who want to build a usable agent system, not just
read the package list.

Start here:

1. [Getting Started](getting-started.md) - create a small .NET agent host and
   choose the first packages to install.
2. [Core Concepts](core-concepts.md) - understand agents, tools, workflows,
   memory, governance, and evidence.
3. [Package Map](package-map.md) - choose the package family that fits the job.
4. [MCP and A2A](mcp-and-a2a.md) - expose tools to humans and agents through
   stable protocol surfaces.
5. [Governance and Observability](governance-and-observability.md) - add
   approval, telemetry, policy, and review hooks before automation becomes
   high impact.
6. [Open Core and Premium Path](open-core-and-premium.md) - understand what is
   public, what is optional, and what belongs in commercial layers.

Companion docs:

- Plugins: `dotnetagents-plugins/docs`
- Examples: `dotnetagents-examples/docs`
- Comparison guide: [`../COMPARISON.md`](../COMPARISON.md)

## How To Read This Set

If you are new to agent systems, read Getting Started and Core Concepts first.
If you already know the shape of the product you are building, jump to Package
Map and MCP and A2A. If you are moving toward production, read Governance and
Observability before wiring tools that mutate data or call external systems.

The docs intentionally stay at the public framework level. They may mention
premium packages and private agent repositories as product-level examples, but
they do not include private code, private datasets, scoring internals, or
operator runbooks.
