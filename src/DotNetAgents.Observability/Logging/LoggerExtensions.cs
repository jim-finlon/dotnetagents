// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace DotNetAgents.Observability.Logging;

/// <summary>
/// Extension methods for structured logging.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs an LLM call with structured data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model name.</param>
    /// <param name="inputTokens">The number of input tokens.</param>
    /// <param name="outputTokens">The number of output tokens.</param>
    /// <param name="duration">The duration of the call.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public static void LogLLMCall(
        this ILogger logger,
        string model,
        int inputTokens,
        int outputTokens,
        TimeSpan duration,
        string? correlationId = null)
    {
        logger.LogInformation(
            "LLM call completed. Model: {Model}, InputTokens: {InputTokens}, OutputTokens: {OutputTokens}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
            model,
            inputTokens,
            outputTokens,
            duration.TotalMilliseconds,
            correlationId);
    }

    /// <summary>
    /// Logs a workflow execution start.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workflowName">The name of the workflow.</param>
    /// <param name="runId">The workflow run ID.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public static void LogWorkflowStart(
        this ILogger logger,
        string workflowName,
        string runId,
        string? correlationId = null)
    {
        logger.LogInformation(
            "Workflow execution started. Workflow: {WorkflowName}, RunId: {RunId}, CorrelationId: {CorrelationId}",
            workflowName,
            runId,
            correlationId);
    }

    /// <summary>
    /// Logs a workflow execution completion.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workflowName">The name of the workflow.</param>
    /// <param name="runId">The workflow run ID.</param>
    /// <param name="duration">The duration of the execution.</param>
    /// <param name="iterations">The number of iterations.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public static void LogWorkflowComplete(
        this ILogger logger,
        string workflowName,
        string runId,
        TimeSpan duration,
        int iterations,
        string? correlationId = null)
    {
        logger.LogInformation(
            "Workflow execution completed. Workflow: {WorkflowName}, RunId: {RunId}, Duration: {Duration}ms, Iterations: {Iterations}, CorrelationId: {CorrelationId}",
            workflowName,
            runId,
            duration.TotalMilliseconds,
            iterations,
            correlationId);
    }

    /// <summary>
    /// Logs a checkpoint creation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="checkpointId">The checkpoint ID.</param>
    /// <param name="runId">The workflow run ID.</param>
    /// <param name="nodeName">The node name where the checkpoint was created.</param>
    public static void LogCheckpointCreated(
        this ILogger logger,
        string checkpointId,
        string runId,
        string nodeName)
    {
        logger.LogDebug(
            "Checkpoint created. CheckpointId: {CheckpointId}, RunId: {RunId}, NodeName: {NodeName}",
            checkpointId,
            runId,
            nodeName);
    }

    /// <summary>
    /// Logs a tool execution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="duration">The duration of the execution.</param>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public static void LogToolExecution(
        this ILogger logger,
        string toolName,
        TimeSpan duration,
        bool success,
        string? correlationId = null)
    {
        var level = success ? LogLevel.Information : LogLevel.Warning;
        logger.Log(
            level,
            "Tool execution {Status}. Tool: {ToolName}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
            success ? "completed" : "failed",
            toolName,
            duration.TotalMilliseconds,
            correlationId);
    }

    /// <summary>
    /// Logs cost information.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model name.</param>
    /// <param name="cost">The cost in USD.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    public static void LogCost(
        this ILogger logger,
        string model,
        decimal cost,
        string? correlationId = null)
    {
        logger.LogInformation(
            "Cost recorded. Model: {Model}, Cost: ${Cost:F4}, CorrelationId: {CorrelationId}",
            model,
            cost,
            correlationId);
    }
}
