namespace DotNetAgents.Abstractions.PublicSubstitutes.Evals;

/// <summary>
/// Records public eval-run summaries for examples, CI, and private-factory adapters.
/// Implementations expose flattened pass/fail/score data only; private scoring
/// rubrics and experiment lineage stay behind adapter boundaries.
/// </summary>
public interface IPublicEvalRunRecorder
{
    ValueTask<PublicEvalRunHandle> StartAsync(
        PublicEvalRunRequest request,
        CancellationToken cancellationToken = default);

    ValueTask RecordCaseAsync(
        PublicEvalRunHandle handle,
        PublicEvalCaseResult result,
        CancellationToken cancellationToken = default);

    ValueTask<PublicEvalRunSummary> CompleteAsync(
        PublicEvalRunHandle handle,
        CancellationToken cancellationToken = default);

    ValueTask<PublicEvalRunRecord?> GetAsync(
        PublicEvalRunHandle handle,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<PublicEvalRunRecord> ListAsync(
        PublicEvalRunQuery? query = null,
        CancellationToken cancellationToken = default);
}
