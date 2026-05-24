; Unshipped analyzer release.
; SPDX-License-Identifier: Apache-2.0
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID  | Category                    | Severity | Notes
---------|-----------------------------|----------|--------------------------------------------------------------------------------
DNA00101 | DotNetAgents.Workflow       | Error    | Workflow graph must have an entry point.
DNA00102 | DotNetAgents.Workflow       | Error    | Workflow graph must have at least one exit point.
DNA00103 | DotNetAgents.Workflow       | Warning  | Workflow graph contains unreachable nodes.
DNA00104 | DotNetAgents.Workflow       | Warning  | Node has no outgoing edges and is not an exit point.
DNA00201 | DotNetAgents.StateMachine   | Error    | State machine must have an initial state.
DNA00202 | DotNetAgents.StateMachine   | Error    | State machine transition references non-existent state.
DNA00203 | DotNetAgents.StateMachine   | Warning  | State machine contains unreachable states.
