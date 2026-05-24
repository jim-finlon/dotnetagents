// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.Mcp.Server;

public interface IDnaObservabilityPublisher
{
    Task<string?> PublishAsync(DnaObservabilityEnvelopeRequest request, CancellationToken cancellationToken = default);
}

public sealed class NoOpDnaObservabilityPublisher : IDnaObservabilityPublisher
{
    public static NoOpDnaObservabilityPublisher Instance { get; } = new();

    private NoOpDnaObservabilityPublisher()
    {
    }

    public Task<string?> PublishAsync(DnaObservabilityEnvelopeRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
