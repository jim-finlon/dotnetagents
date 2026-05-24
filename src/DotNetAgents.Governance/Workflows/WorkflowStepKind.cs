// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Governance.Workflows;

/// <summary>
/// The v1 primitive shapes of a step in an agent workflow. Each step in a workflow
/// definition carries one of these kinds so the runner can dispatch and so policy
/// (memory-scope, retry, audit, approval) can vary per kind.
/// </summary>
public enum WorkflowStepKind
{
    /// <summary>Entry condition: cron, event, MCP call, webhook.</summary>
    Trigger = 0,
    /// <summary>Fetch data: RAG query, DB query, API call.</summary>
    Retrieve = 1,
    /// <summary>LLM reasoning / planning over retrieved context.</summary>
    Reason = 2,
    /// <summary>Read the selected full source after retrieval and relevance reasoning.</summary>
    ReadSource = 3,
    /// <summary>Side-effect: POST, MCP tool call, DB write, message publish.</summary>
    Act = 4,
    /// <summary>Produce the user-facing output (chat reply, email body, story comment).</summary>
    Respond = 5,
    /// <summary>Deterministic or reviewable policy decision before a sensitive branch or action.</summary>
    PolicyGate = 6,
    /// <summary>Human approval, selection, or confirmation before the workflow continues.</summary>
    HumanConfirm = 7,

    /// <summary>Legacy alias for <see cref="Reason"/>.</summary>
    Think = Reason,
    /// <summary>Legacy alias for <see cref="ReadSource"/>.</summary>
    Read = ReadSource
}
