// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Abstractions.Hooks;

/// <summary>
/// The lifecycle moments at which agent hooks are invoked. Hooks subscribe to one or more
/// checkpoints and run synchronously (in priority order) at each subscribed moment.
/// </summary>
/// <remarks>
/// The seven canonical checkpoints are taken from the Anthropic Claude Agent SDK convention,
/// which the field is converging on as the de facto "agent OS" extensibility model.
/// Implementations MAY define additional checkpoints; consumers MUST tolerate unknown values.
/// </remarks>
public enum HookCheckpoint
{
    /// <summary>Before a tool is invoked. Hook can Block (deny tool execution) or Redact (replace tool input).</summary>
    PreToolUse = 0,

    /// <summary>After a tool returns. Hook can Redact (rewrite tool output before the agent sees it) but cannot Block (tool already executed).</summary>
    PostToolUse = 1,

    /// <summary>Before an LLM call is dispatched. Hook can Block (deny LLM call) or Redact (modify the prompt/messages).</summary>
    PreLlmCall = 2,

    /// <summary>After an LLM call returns. Hook can Redact (rewrite the model's output) but cannot Block (call already happened).</summary>
    PostLlmCall = 3,

    /// <summary>When the agent's context is being summarized/compacted. Hook can observe the compaction inputs but cannot Block (compaction is mandatory at certain context-pressure thresholds).</summary>
    PreCompact = 4,

    /// <summary>When any new message enters the agent's context. Hook can Redact but rarely Block.</summary>
    OnMessage = 5,

    /// <summary>When the agent loop encounters an exception. Hook can observe and append to evidence; Block here means re-raise after observation.</summary>
    OnError = 6,
}
