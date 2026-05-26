# Open Core And Premium Path

DotNetAgents is open core.

The public repositories are meant to be useful by themselves. You can build
agents, workflows, protocol servers, tools, and examples without private code.

## Public Core

Public core includes:

- runtime and hosting primitives
- agent patterns
- MCP and A2A contracts
- workflow and task primitives
- memory and session abstractions
- structured output and model-routing contracts
- governance and observability building blocks
- public examples and public plugins

## Premium And Private Layers

Commercial layers can build on the public core with:

- managed operations
- premium adapters
- enterprise governance packs
- certification receipts
- hosted evaluation and evidence
- semi- or fully automatic self-improving software factory operation, with
  human-in-the-loop review available where teams want it
- laboratory, simulator, and Arena environments for hardening agents before
  production use (Note: The public repository includes a safe, offline `sales-arena`
  teaser that demonstrates ledger events, leaderboard computation, and replay
  reporting without exposing proprietary scoring, datasets, or genetic optimizer mechanics)
- vertical templates
- support and private package feeds

The same public primitives are used in private agent repositories to run larger
governed workflows. This repository does not include the private control plane,
premium code, proprietary scoring, private datasets, or operating procedures.

## Public Documentation Boundary

Public docs can explain:

- what a capability is for
- which public package or interface to start with
- how to configure a safe local example
- where premium offerings add managed value

Public docs should not explain:

- private service topology
- internal workflow operations
- proprietary scoring or optimization details
- private datasets
- customer-specific adapters
- credential recovery procedures
- operator-only runbooks

That boundary keeps the public framework useful while preserving the commercial
advantage of the premium operating layer.
