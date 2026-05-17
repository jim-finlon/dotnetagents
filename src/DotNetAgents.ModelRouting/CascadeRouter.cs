namespace DotNetAgents.ModelRouting;

/// <summary>Cascade router implementation: invokes a delegate per tier to get confidence, escalates when below threshold. FR-MR-001.</summary>
public sealed class CascadeRouter : ICascadeRouter
{
    private readonly List<(string ModelId, double ConfidenceThreshold)> _tiers = new();
    private readonly Func<string, RoutingRequest, CancellationToken, Task<double?>> _getConfidence;

    /// <summary>Builds a cascade router that uses the given delegate to obtain confidence for a model's response.</summary>
    /// <param name="getConfidence">Async delegate (modelId, request, ct) => confidence 0–1, or null if not available (treat as accept).</param>
    public CascadeRouter(Func<string, RoutingRequest, CancellationToken, Task<double?>>? getConfidence = null)
    {
        _getConfidence = getConfidence ?? ((_, _, _) => Task.FromResult<double?>(null));
    }

    /// <inheritdoc />
    public ICascadeRouter AddTier(string modelId, double confidenceThreshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        if (confidenceThreshold < 0 || confidenceThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(confidenceThreshold), "Must be between 0 and 1.");
        _tiers.Add((modelId, confidenceThreshold));
        return this;
    }

    /// <inheritdoc />
    public async Task<RoutingResult> RouteAsync(RoutingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        for (var i = 0; i < _tiers.Count; i++)
        {
            var (modelId, threshold) = _tiers[i];
            var confidence = await _getConfidence(modelId, request, cancellationToken).ConfigureAwait(false);
            if (confidence == null || confidence >= threshold)
                return new RoutingResult { ModelId = modelId, Confidence = confidence, Source = $"cascade-tier-{i}" };
        }
        var fallback = _tiers.Count > 0 ? _tiers[^1].ModelId : string.Empty;
        return new RoutingResult { ModelId = fallback, Source = "cascade-fallback" };
    }
}
