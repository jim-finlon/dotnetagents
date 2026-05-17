using DotNetAgents.Abstractions.Prompts;
using DotNetAgents.Core.Prompts;

namespace DotNetAgents.Core.Agents;

/// <summary>
/// Default prompt template for ReAct agent pattern.
/// </summary>
public class ReActPromptTemplate : PromptTemplate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReActPromptTemplate"/> class with the default ReAct prompt.
    /// </summary>
    public ReActPromptTemplate()
        : base(GetDefaultReActPrompt())
    {
    }

    private static string GetDefaultReActPrompt()
    {
        return @"You are a helpful assistant that can use tools to answer questions.

Available tools:
{tools}

Use the following format:

Question: the input question you must answer
Thought: you should always think about what to do
Action: the action to take, should be one of [{tool_names}]
Action Input: the input to the action
Observation: the result of the action
... (this Thought/Action/Action Input/Observation can repeat N times)
Thought: I now know the final answer
Final Answer: the final answer to the original input question

{conversation_history}

Question: {input}
Thought:";
    }
}
