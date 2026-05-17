using DotNetAgents.Abstractions.Agents;

namespace DotNetAgents.Core.Agents;

/// <summary>
/// Creates configured agent instances using a caller-provided factory delegate.
/// </summary>
/// <typeparam name="TAgent">The concrete agent type.</typeparam>
/// <typeparam name="TConfiguration">The caller-defined configuration snapshot type.</typeparam>
public sealed class DelegateAgentInstanceFactory<TAgent, TConfiguration> :
    IAgentInstanceFactory<TAgent, TConfiguration>
    where TAgent : IAgent
{
    private readonly Func<AgentInstanceRequest<TConfiguration>, CancellationToken, ValueTask<TAgent>> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateAgentInstanceFactory{TAgent, TConfiguration}"/> class.
    /// </summary>
    /// <param name="factory">Delegate that creates the concrete agent for a validated request.</param>
    public DelegateAgentInstanceFactory(
        Func<AgentInstanceRequest<TConfiguration>, CancellationToken, ValueTask<TAgent>> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public async ValueTask<AgentRuntimeInstance<TAgent, TConfiguration>> CreateAsync(
        AgentInstanceRequest<TConfiguration> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var agent = await _factory(request, cancellationToken).ConfigureAwait(false);

        return new AgentRuntimeInstance<TAgent, TConfiguration>
        {
            Identity = request.Identity,
            Agent = agent ?? throw new InvalidOperationException("Agent factory returned null."),
            Configuration = request.Configuration,
            ConfigurationBindings = request.ConfigurationBindings,
            ModelBindings = request.ModelBindings,
            ToolBindings = request.ToolBindings,
            Correlation = request.Correlation,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
