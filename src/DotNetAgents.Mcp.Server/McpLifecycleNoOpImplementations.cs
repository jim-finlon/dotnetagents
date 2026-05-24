// SPDX-License-Identifier: Apache-2.0

using DotNetAgents.Mcp.Models;

namespace DotNetAgents.Mcp.Server;

public sealed class NoOpMcpEventSink : IMcpEventSink
{
    public static readonly NoOpMcpEventSink Instance = new();

    private NoOpMcpEventSink()
    {
    }

    public Task OnToolCallStartedAsync(McpToolCallContext context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnToolCallCompletedAsync(
        McpToolCallContext context,
        McpToolCallResponse response,
        McpToolLifecycleEvent lifecycleEvent,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class NoOpMcpSafetyVerifier : IMcpSafetyVerifier
{
    public static readonly NoOpMcpSafetyVerifier Instance = new();

    private NoOpMcpSafetyVerifier()
    {
    }

    public Task<McpSafetyVerificationResult> VerifyBeforeCallAsync(
        McpToolCallContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult(McpSafetyVerificationResult.Pass());

    public Task<McpSafetyVerificationResult> VerifyAfterCallAsync(
        McpToolCallContext context,
        McpToolCallResponse response,
        CancellationToken cancellationToken = default)
        => Task.FromResult(McpSafetyVerificationResult.Pass());
}

public sealed class NoOpLessonEventPublisher : ILessonEventPublisher
{
    public static readonly NoOpLessonEventPublisher Instance = new();

    private NoOpLessonEventPublisher()
    {
    }

    public Task PublishAsync(LessonEvent lessonEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
