# Package Map

Use this map to choose packages by job. Package names may move while the preview
train hardens, but the categories are stable.

## Runtime And Hosting

| Package Area | Use It For |
| --- | --- |
| `DotNetAgents.Core` | core abstractions, agent execution primitives, tool contracts |
| `DotNetAgents.Hosting` | ASP.NET Core host composition, health, startup receipts |
| `DotNetAgents.Runtime` | session and trajectory records, runtime turns, replayable evidence |
| `DotNetAgents.Configuration` | option binding and configuration helpers |
| `DotNetAgents.Contracts` | shared contracts for agent profiles and validation |

## Agent Patterns

| Package Area | Use It For |
| --- | --- |
| `DotNetAgents.Agents.StateMachines` | explicit state transitions |
| `DotNetAgents.Agents.BehaviorTrees` | tactical decision trees |
| `DotNetAgents.Agents.Supervisor` | routing and delegation patterns |
| `DotNetAgents.Agents.Swarm` | collaborative multi-agent patterns |
| `DotNetAgents.Agents.WorkerPool` | bounded parallel task execution |
| `DotNetAgents.Agents.Hierarchical` | organized teams of agents |
| `DotNetAgents.Subagents` | child task execution and summary return |

## Protocols And Tools

| Package Area | Use It For |
| --- | --- |
| `DotNetAgents.Mcp` | MCP client abstractions |
| `DotNetAgents.Mcp.Server` | HTTP MCP server endpoints |
| `DotNetAgents.Mcp.Auth` | MCP auth primitives |
| `DotNetAgents.Mcp.Stdio` | stdio transport hosting |
| `DotNetAgents.A2A` | A2A contracts |
| `DotNetAgents.A2A.Client` | calling A2A agents |
| `DotNetAgents.A2A.Server` | exposing an A2A agent endpoint |
| `DotNetAgents.Tools.*` | built-in and development tool contracts |

## Intelligence Layer

| Package Area | Use It For |
| --- | --- |
| `DotNetAgents.ModelRouting` | provider and model selection policy |
| `DotNetAgents.PromptRuntime` | prompt/runtime separation |
| `DotNetAgents.StructuredOutput` | JSON and typed output handling |
| `DotNetAgents.AgenticRAG` | retrieval-augmented agent flows |
| `DotNetAgents.Knowledge` | public knowledge abstractions |
| `DotNetAgents.CognitiveMesh` | bounded cognitive graph contracts |

## Governance And Operations

| Package Area | Use It For |
| --- | --- |
| `DotNetAgents.Governance` | policy and taxonomy primitives |
| `DotNetAgents.Security` | redaction and safe-output helpers |
| `DotNetAgents.PreviewConfirm` | preview/confirm workflows |
| `DotNetAgents.ReleasePolicy` | release gate concepts |
| `DotNetAgents.LaneOps` | lane-oriented operation metadata |
| `DotNetAgents.Compliance.Predicates` | compliance predicate building blocks |

## Developer And Multimodal Surfaces

| Package Area | Use It For |
| --- | --- |
| `DotNetAgents.CLI` | command-line surfaces |
| `DotNetAgents.REPL` | local exploration |
| `DotNetAgents.Analyzers` | build-time guidance |
| `DotNetAgents.Documents` | document-oriented contracts |
| `DotNetAgents.MultiModal` | multimodal primitives |
| `DotNetAgents.Voice.*` | voice command and dialog workflows |

For adapters that touch storage, vector stores, messaging, UI, media providers,
or browser/computer-use systems, use the plugins repository.
