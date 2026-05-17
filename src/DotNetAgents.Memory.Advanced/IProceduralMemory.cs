namespace DotNetAgents.Memory.Advanced;

/// <summary>Procedural memory: learn/recall/refine procedures. FR-MEM-003.</summary>
public interface IProceduralMemory
{
    Task LearnProcedureAsync(string name, string goal, IReadOnlyList<Step> steps, CancellationToken cancellationToken = default);
    Task<Procedure?> RecallProcedureAsync(string name, CancellationToken cancellationToken = default);
    Task RefineProcedureAsync(string name, string feedback, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Procedure>> FindSimilarProceduresAsync(string goal, int limit = 10, CancellationToken cancellationToken = default);
}
