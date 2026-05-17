namespace DotNetAgents.Workflow.Session.Bootstrap;

/// <summary>
/// Interface for generating bootstrap payloads for session resumption.
/// </summary>
public interface IBootstrapGenerator
{
    /// <summary>
    /// Generates a bootstrap payload from the provided data.
    /// </summary>
    /// <param name="data">The bootstrap data.</param>
    /// <param name="format">The output format.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The generated bootstrap payload.</returns>
    Task<BootstrapPayload> GenerateAsync(
        BootstrapData data,
        BootstrapFormat format = BootstrapFormat.Json,
        CancellationToken cancellationToken = default);
}
