using System.Diagnostics;
using DotNetAgents.Agents.Messaging;
using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;

namespace DotNetAgents.Observability.Agents;

/// <summary>
/// OpenTelemetry extensions for agent communication tracing.
/// </summary>
public static class AgentTracingExtensions
{
    private static readonly ActivitySource ActivitySource = new("DotNetAgents.Agents");

    /// <summary>
    /// Starts an activity for agent message sending.
    /// </summary>
    /// <param name="message">The message being sent.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartAgentMessageActivity(
        AgentMessage message,
        string? correlationId = null)
    {
        var activity = ActivitySource.StartActivity("agent.message.send");
        if (activity != null)
        {
            activity.SetTag("agent.message.id", message.MessageId);
            activity.SetTag("agent.message.type", message.MessageType);
            activity.SetTag("agent.message.from", message.FromAgentId);
            activity.SetTag("agent.message.to", message.ToAgentId);
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                activity.SetTag("correlation.id", correlationId);
            }
            if (message.CorrelationId != null)
            {
                activity.SetTag("message.correlation.id", message.CorrelationId);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for agent registration.
    /// </summary>
    /// <param name="agentInfo">The agent being registered.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartAgentRegistrationActivity(AgentInfo agentInfo)
    {
        var activity = ActivitySource.StartActivity("agent.registry.register");
        if (activity != null)
        {
            activity.SetTag("agent.id", agentInfo.AgentId);
            activity.SetTag("agent.type", agentInfo.AgentType);
            activity.SetTag("agent.status", agentInfo.Status.ToString());
            activity.SetTag("agent.capabilities.tools", string.Join(",", agentInfo.Capabilities.SupportedTools));
            activity.SetTag("agent.capabilities.intents", string.Join(",", agentInfo.Capabilities.SupportedIntents));
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for task submission.
    /// </summary>
    /// <param name="task">The task being submitted.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartTaskSubmissionActivity(
        WorkerTask task,
        string? correlationId = null)
    {
        var activity = ActivitySource.StartActivity("agent.task.submit");
        if (activity != null)
        {
            activity.SetTag("task.id", task.TaskId);
            activity.SetTag("task.type", task.TaskType);
            activity.SetTag("task.priority", task.Priority);
            if (!string.IsNullOrWhiteSpace(task.RequiredCapability))
            {
                activity.SetTag("task.required_capability", task.RequiredCapability);
            }
            if (!string.IsNullOrWhiteSpace(task.PreferredAgentId))
            {
                activity.SetTag("task.preferred_agent", task.PreferredAgentId);
            }
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                activity.SetTag("correlation.id", correlationId);
            }
        }

        return activity;
    }

    /// <summary>
    /// Starts an activity for task execution.
    /// </summary>
    /// <param name="task">The task being executed.</param>
    /// <param name="workerAgentId">The ID of the worker agent executing the task.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartTaskExecutionActivity(
        WorkerTask task,
        string workerAgentId)
    {
        var activity = ActivitySource.StartActivity("agent.task.execute");
        if (activity != null)
        {
            activity.SetTag("task.id", task.TaskId);
            activity.SetTag("task.type", task.TaskType);
            activity.SetTag("worker.agent.id", workerAgentId);
        }

        return activity;
    }

    /// <summary>
    /// Records task completion in the current activity.
    /// </summary>
    /// <param name="result">The task result.</param>
    /// <param name="activity">The activity to record on.</param>
    public static void RecordTaskCompletion(
        WorkerTaskResult result,
        Activity? activity)
    {
        if (activity == null)
            return;

        activity.SetTag("task.result.success", result.Success);
        activity.SetTag("task.execution_time_ms", result.ExecutionTime.TotalMilliseconds);
        if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            activity.SetTag("task.error", result.ErrorMessage);
            activity.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }
    }

    /// <summary>
    /// Starts an activity for worker pool operations.
    /// </summary>
    /// <param name="operation">The operation name (e.g., "add_worker", "remove_worker", "get_worker").</param>
    /// <param name="agentId">The agent ID involved in the operation.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartWorkerPoolActivity(
        string operation,
        string? agentId = null)
    {
        var activity = ActivitySource.StartActivity($"worker.pool.{operation}");
        if (activity != null && !string.IsNullOrWhiteSpace(agentId))
        {
            activity.SetTag("agent.id", agentId);
        }

        return activity;
    }
}
