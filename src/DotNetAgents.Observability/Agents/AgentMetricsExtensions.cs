using DotNetAgents.Agents.Registry;
using DotNetAgents.Agents.Tasks;
using DotNetAgents.Agents.WorkerPool;
using DotNetAgents.Observability.Metrics;

namespace DotNetAgents.Observability.Agents;

/// <summary>
/// Metrics collection extensions for agent operations.
/// </summary>
public static class AgentMetricsExtensions
{
    /// <summary>
    /// Records metrics for agent message sending.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector.</param>
    /// <param name="messageType">The type of message.</param>
    /// <param name="success">Whether the send was successful.</param>
    /// <param name="duration">The duration of the send operation.</param>
    public static void RecordAgentMessageMetric(
        this IMetricsCollector metricsCollector,
        string messageType,
        bool success,
        TimeSpan duration)
    {
        metricsCollector.IncrementCounter("agent.message.send.count", 1, new Dictionary<string, object>
        {
            ["message_type"] = messageType,
            ["success"] = success.ToString()
        });

        metricsCollector.RecordLatency("agent.message.send", duration, new Dictionary<string, object>
        {
            ["message_type"] = messageType
        });
    }

    /// <summary>
    /// Records metrics for task submission.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector.</param>
    /// <param name="taskType">The type of task.</param>
    /// <param name="priority">The task priority.</param>
    public static void RecordTaskSubmissionMetric(
        this IMetricsCollector metricsCollector,
        string taskType,
        int priority)
    {
        metricsCollector.IncrementCounter("agent.task.submit.count", 1, new Dictionary<string, object>
        {
            ["task_type"] = taskType,
            ["priority"] = priority.ToString()
        });
    }

    /// <summary>
    /// Records metrics for task execution.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector.</param>
    /// <param name="task">The task that was executed.</param>
    /// <param name="result">The task result.</param>
    public static void RecordTaskExecutionMetric(
        this IMetricsCollector metricsCollector,
        WorkerTask task,
        WorkerTaskResult result)
    {
        metricsCollector.IncrementCounter("agent.task.execute.count", 1, new Dictionary<string, object>
        {
            ["task_type"] = task.TaskType,
            ["success"] = result.Success.ToString()
        });

        metricsCollector.RecordLatency("agent.task.execute", result.ExecutionTime, new Dictionary<string, object>
        {
            ["task_type"] = task.TaskType,
            ["success"] = result.Success.ToString()
        });
    }

    /// <summary>
    /// Records metrics for worker pool statistics.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector.</param>
    /// <param name="statistics">The worker pool statistics.</param>
    public static void RecordWorkerPoolMetrics(
        this IMetricsCollector metricsCollector,
        WorkerPoolStatistics statistics)
    {
        metricsCollector.RecordGauge("worker.pool.total_workers", (double)statistics.TotalWorkers);
        metricsCollector.RecordGauge("worker.pool.available_workers", (double)statistics.AvailableWorkers);
        metricsCollector.RecordGauge("worker.pool.busy_workers", (double)statistics.BusyWorkers);
        metricsCollector.RecordGauge("worker.pool.tasks_processed", (double)statistics.TotalTasksProcessed);

        if (statistics.AverageTaskDuration != TimeSpan.Zero)
        {
            metricsCollector.RecordLatency("worker.pool.avg_task_duration", statistics.AverageTaskDuration);
        }
    }

    /// <summary>
    /// Records metrics for agent registry operations.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector.</param>
    /// <param name="operation">The operation name (register, unregister, update).</param>
    /// <param name="agentType">The type of agent.</param>
    public static void RecordAgentRegistryMetric(
        this IMetricsCollector metricsCollector,
        string operation,
        string agentType)
    {
        metricsCollector.IncrementCounter("agent.registry.operation.count", 1, new Dictionary<string, object>
        {
            ["operation"] = operation,
            ["agent_type"] = agentType
        });
    }
}
