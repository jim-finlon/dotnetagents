namespace DotNetAgents.Abstractions.Chains;

/// <summary>
/// Options for runnable execution.
/// </summary>
public record RunnableOptions
{
    /// <summary>
    /// Gets or sets whether to include execution metadata in the result.
    /// </summary>
    public bool IncludeMetadata { get; init; }

    /// <summary>
    /// Gets or sets tags for this execution.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Gets or sets the stable chain trace envelope propagated across runnable boundaries.
    /// </summary>
    public ChainTraceEnvelope? TraceEnvelope { get; init; }

    /// <summary>
    /// Gets or sets additional options specific to the runnable implementation.
    /// </summary>
    public IDictionary<string, object>? AdditionalOptions { get; init; }

    /// <summary>
    /// Creates a copy of the options with an updated trace envelope.
    /// </summary>
    public RunnableOptions WithTraceEnvelope(ChainTraceEnvelope traceEnvelope)
    {
        ArgumentNullException.ThrowIfNull(traceEnvelope);
        return this with { TraceEnvelope = traceEnvelope };
    }
}
