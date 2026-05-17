namespace DotNetAgents.Governance.Identity;

/// <summary>
/// Ambient access to the <see cref="InvokerContext"/> for the current async flow.
/// Injected into repositories, MCP middleware, and HTTP delegating handlers so they can
/// scope their behavior without threading the context through every method signature.
/// </summary>
public interface IInvokerContextAccessor
{
    /// <summary>
    /// The invoker for the current async flow, or null when no context has been set
    /// (e.g. a background job running under service identity). Callers must fail closed
    /// rather than treat "null" as "allow all".
    /// </summary>
    InvokerContext? Current { get; set; }
}
