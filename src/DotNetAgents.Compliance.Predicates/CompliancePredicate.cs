using System.Text.Json;

namespace DotNetAgents.Compliance.Predicates;

public abstract class CompliancePredicate
{
    public abstract string PredicateId { get; }

    public abstract CompliancePredicateResult Evaluate(CompliancePredicateContext context);

    protected static string ReadAllText(JsonElement payload)
    {
        if (payload.ValueKind is JsonValueKind.String)
            return payload.GetString() ?? "";

        return payload.GetRawText();
    }
}

public sealed record CompliancePredicateContext(
    string SubjectRef,
    JsonElement Payload,
    IReadOnlyDictionary<string, string>? Parameters = null);

public sealed record CompliancePredicateResult(
    bool Passed,
    string Summary,
    string? EvidenceRef = null)
{
    public static CompliancePredicateResult Pass(string summary, string? evidenceRef = null) => new(true, summary, evidenceRef);
    public static CompliancePredicateResult Fail(string summary, string? evidenceRef = null) => new(false, summary, evidenceRef);
}

public sealed class CompliancePredicateRegistry
{
    private readonly Dictionary<string, CompliancePredicate> _predicates = new(StringComparer.OrdinalIgnoreCase);

    public CompliancePredicateRegistry(IEnumerable<CompliancePredicate>? predicates = null)
    {
        foreach (var predicate in predicates ?? DefaultPredicates())
            Register(predicate);
    }

    public IReadOnlyCollection<CompliancePredicate> Predicates => _predicates.Values;

    public void Register(CompliancePredicate predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicates[predicate.PredicateId] = predicate;
    }

    public bool TryGet(string predicateId, out CompliancePredicate predicate) =>
        _predicates.TryGetValue(predicateId, out predicate!);

    public static IReadOnlyList<CompliancePredicate> DefaultPredicates() =>
    [
        new PhiDetectorPredicate(),
        new AuditTrailPresentPredicate(),
        new SecretAtRestPredicate()
    ];
}
