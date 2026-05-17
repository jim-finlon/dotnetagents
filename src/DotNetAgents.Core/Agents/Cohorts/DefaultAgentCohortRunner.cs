using DotNetAgents.Abstractions.Agents;
using DotNetAgents.Abstractions.Agents.Cohorts;

namespace DotNetAgents.Core.Agents.Cohorts;

/// <summary>
/// Default in-process cohort runner. It preserves member evidence and failure isolation.
/// </summary>
/// <typeparam name="TAgent">The concrete agent type.</typeparam>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public sealed class DefaultAgentCohortRunner<TAgent, TConfiguration> :
    IAgentCohortRunner<TAgent, TConfiguration>
    where TAgent : IAgent
{
    /// <inheritdoc />
    public async ValueTask<AgentCohortRunResult> RunAsync(
        AgentCohortDefinition<TConfiguration> definition,
        IAgentInstanceFactory<TAgent, TConfiguration> instanceFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(instanceFactory);
        definition.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = DateTimeOffset.UtcNow;
        var runId = !string.IsNullOrWhiteSpace(definition.Correlation.RunId)
            ? definition.Correlation.RunId!
            : Guid.NewGuid().ToString("N");

        var memberResults = new List<AgentCohortMemberResult>(definition.Members.Count);

        foreach (var member in definition.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memberStartedAt = DateTimeOffset.UtcNow;

            try
            {
                var instance = await instanceFactory
                    .CreateAsync(member.InstanceRequest, cancellationToken)
                    .ConfigureAwait(false);

                var input = !string.IsNullOrWhiteSpace(member.InputOverride)
                    ? member.InputOverride!
                    : definition.SharedTask.Input;

                var stepResult = await instance.Agent
                    .ExecuteStepAsync(input, cancellationToken)
                    .ConfigureAwait(false);

                memberResults.Add(new AgentCohortMemberResult
                {
                    MemberId = member.MemberId,
                    Role = member.Role,
                    Identity = instance.Identity,
                    IsSuccess = true,
                    Output = stepResult.Output,
                    StartedAt = memberStartedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Metadata = MergeMetadata(member.Metadata, new Dictionary<string, string>
                    {
                        ["taskId"] = definition.SharedTask.TaskId,
                        ["shouldContinue"] = stepResult.ShouldContinue.ToString()
                    })
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                memberResults.Add(new AgentCohortMemberResult
                {
                    MemberId = member.MemberId,
                    Role = member.Role,
                    Identity = member.InstanceRequest.Identity,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    StartedAt = memberStartedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Metadata = MergeMetadata(member.Metadata, new Dictionary<string, string>
                    {
                        ["taskId"] = definition.SharedTask.TaskId,
                        ["exceptionType"] = ex.GetType().Name
                    })
                });

                if (definition.IsolationPolicy.FailureMode == AgentCohortFailureMode.StopOnFirstFailure)
                {
                    break;
                }
            }
        }

        var failedCount = memberResults.Count(result => !result.IsSuccess);
        var succeededCount = memberResults.Count(result => result.IsSuccess);

        return new AgentCohortRunResult
        {
            CohortId = definition.CohortId,
            RunId = runId,
            Correlation = definition.Correlation with { RunId = runId },
            ResultAggregationPolicy = definition.ResultAggregationPolicy,
            IsSuccess = failedCount == 0 && memberResults.Count == definition.Members.Count,
            SucceededMemberCount = succeededCount,
            FailedMemberCount = failedCount,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            MemberResults = memberResults
        };
    }

    private static IReadOnlyDictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string> memberMetadata,
        IReadOnlyDictionary<string, string> runtimeMetadata)
    {
        var merged = new Dictionary<string, string>(memberMetadata, StringComparer.OrdinalIgnoreCase);
        foreach (var item in runtimeMetadata)
        {
            merged[item.Key] = item.Value;
        }

        return merged;
    }
}
