namespace DotNetAgents.ModelRouting;

/// <summary>Selects the first available model that has all required capabilities. FR-MR-002.</summary>
public sealed class CapabilityRouter : ICapabilityRouter
{
    private readonly List<ModelCapabilities> _models = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Register(ModelCapabilities model)
    {
        ArgumentNullException.ThrowIfNull(model);
        lock (_lock)
        {
            var idx = _models.FindIndex(m => string.Equals(m.ModelId, model.ModelId, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
                _models[idx] = model;
            else
                _models.Add(model);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelCapabilities> GetRegisteredModels()
    {
        lock (_lock) { return _models.ToList(); }
    }

    /// <inheritdoc />
    public Task<RoutingResult> RouteAsync(RoutingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ModelCapabilities[] copy;
        lock (_lock) { copy = _models.ToArray(); }

        var required = request.RequiredCapabilities ?? new HashSet<string>();
        foreach (var model in copy)
        {
            if (!model.Available) continue;
            if (required.Count > 0 && !required.IsSubsetOf(model.Capabilities)) continue;
            return Task.FromResult(new RoutingResult
            {
                ModelId = model.ModelId,
                Endpoint = model.Endpoint,
                Source = "capability"
            });
        }
        var fallback = copy.FirstOrDefault(m => m.Available);
        return Task.FromResult(new RoutingResult
        {
            ModelId = fallback?.ModelId ?? string.Empty,
            Endpoint = fallback?.Endpoint,
            Source = "capability-fallback"
        });
    }
}
