// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace DotNetAgents.Runtime;

/// <summary>
/// Session-scoped agent turn runner with trajectory recording. For multi-iteration ReAct loops use
/// the core agent executor; see <c>docs/architecture/AGENT-EXECUTOR-VS-AGENT-RUNTIME.md</c>.
/// </summary>
public sealed class AgentRuntime : IAgentRuntime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAgentSessionStore _sessionStore;
    private readonly ITrajectoryRecorder _trajectoryRecorder;
    private readonly IAgentTurnModel _model;
    private readonly IToolsetResolver _toolsetResolver;

    public AgentRuntime(
        IAgentSessionStore sessionStore,
        ITrajectoryRecorder trajectoryRecorder,
        IAgentTurnModel model,
        IToolsetResolver toolsetResolver)
    {
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _trajectoryRecorder = trajectoryRecorder ?? throw new ArgumentNullException(nameof(trajectoryRecorder));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _toolsetResolver = toolsetResolver ?? throw new ArgumentNullException(nameof(toolsetResolver));
    }

    public async Task<AgentRunResult> RunAsync(
        AgentRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ActorId))
        {
            throw new ArgumentException("Actor id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            throw new ArgumentException("User input is required.", nameof(request));
        }

        var session = await _sessionStore.CreateSessionAsync(new AgentSession
        {
            ActorId = request.ActorId,
            RunMode = request.RunMode,
            ParentSessionId = request.ParentSessionId,
            DelegatedFromActorId = request.DelegatedFromActorId,
            Status = AgentSessionStatus.Created,
            Metadata = request.Metadata
        }, cancellationToken).ConfigureAwait(false);

        session = await _sessionStore.UpdateSessionStatusAsync(
            session.Id,
            AgentSessionStatus.Running,
            cancellationToken).ConfigureAwait(false);

        await _sessionStore.AppendMessageAsync(new AgentMessage
        {
            SessionId = session.Id,
            Role = AgentMessageRole.User,
            Content = request.UserInput
        }, cancellationToken).ConfigureAwait(false);

        var providerCall = new ProviderCall
        {
            SessionId = session.Id,
            ModelRoute = request.ModelRoute,
            Status = ProviderCallStatus.Started
        };

        try
        {
            var activityBeforeModel = await _sessionStore.ReadActivityAsync(session.Id, cancellationToken).ConfigureAwait(false);
            var modelResponse = await _model.GenerateAsync(
                new AgentModelRequest(session, activityBeforeModel.Messages, request.ModelRoute),
                cancellationToken).ConfigureAwait(false);

            await _sessionStore.AppendProviderCallAsync(providerCall with
            {
                Status = ProviderCallStatus.Succeeded,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                InputTokens = modelResponse.InputTokens,
                OutputTokens = modelResponse.OutputTokens
            }, cancellationToken).ConfigureAwait(false);

            var toolErrorCount = await ExecuteToolCallsAsync(session.Id, modelResponse.ToolCalls ?? [], cancellationToken)
                .ConfigureAwait(false);

            await _sessionStore.AppendMessageAsync(new AgentMessage
            {
                SessionId = session.Id,
                Role = AgentMessageRole.Assistant,
                Content = modelResponse.AssistantMessage
            }, cancellationToken).ConfigureAwait(false);

            var status = toolErrorCount == 0
                ? AgentSessionStatus.Completed
                : AgentSessionStatus.CompletedWithToolErrors;

            return await CompleteRunAsync(session.Id, status, modelResponse.AssistantMessage, request.ModelRoute, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _sessionStore.AppendProviderCallAsync(providerCall with
            {
                Status = ProviderCallStatus.Failed,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            }, cancellationToken).ConfigureAwait(false);

            return await CompleteRunAsync(session.Id, AgentSessionStatus.Failed, string.Empty, request.ModelRoute, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<int> ExecuteToolCallsAsync(
        string sessionId,
        IReadOnlyList<PlannedToolCall> toolCalls,
        CancellationToken cancellationToken)
    {
        var loopResult = await AgentToolLoop.ExecuteAsync(
            toolCalls,
            (name, ct) => _toolsetResolver.ResolveAsync(name, ct),
            async (invocation, ct) => await _sessionStore.AppendToolInvocationAsync(new ToolInvocation
            {
                SessionId = sessionId,
                ToolName = invocation.ToolName,
                ToolCallId = invocation.ToolCallId,
                InputJson = invocation.InputJson,
                OutputJson = invocation.OutputJson,
                Status = invocation.Succeeded ? ToolInvocationStatus.Succeeded : ToolInvocationStatus.Failed,
                StartedAtUtc = invocation.StartedAtUtc,
                CompletedAtUtc = invocation.CompletedAtUtc,
                ErrorMessage = invocation.ErrorMessage
            }, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        return loopResult.ErrorCount;
    }

    private async Task<AgentRunResult> CompleteRunAsync(
        string sessionId,
        AgentSessionStatus status,
        string assistantMessage,
        ModelRouteMetadata modelRoute,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.UpdateSessionStatusAsync(sessionId, status, cancellationToken).ConfigureAwait(false);
        var activity = await _sessionStore.ReadActivityAsync(session.Id, cancellationToken).ConfigureAwait(false);
        var snapshot = await _sessionStore.AppendContextSnapshotAsync(new ContextSnapshot
        {
            SessionId = session.Id,
            MessageCount = activity.Messages.Count,
            ToolInvocationCount = activity.ToolInvocations.Count,
            ProviderCallCount = activity.ProviderCalls.Count
        }, cancellationToken).ConfigureAwait(false);

        activity = activity with { ContextSnapshots = [.. activity.ContextSnapshots, snapshot] };
        var trajectory = await _trajectoryRecorder.RecordAsync(activity, modelRoute, cancellationToken).ConfigureAwait(false);

        return new AgentRunResult(session, status, assistantMessage, trajectory);
    }

    private static string SerializeSafe(object? value) =>
        value is null ? "null" : JsonSerializer.Serialize(value, JsonOptions);
}
