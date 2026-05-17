using DotNetAgents.Governance.Connectors;
using DotNetAgents.Governance.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetAgents.Governance;

/// <summary>
/// DI extensions. Services opt in by calling <see cref="AddDotNetAgentsGovernance"/> and
/// then wiring their boundaries (MCP middleware, HTTP delegating handlers, repositories)
/// to resolve <see cref="IInvokerContextAccessor"/> and <see cref="IConnectorAccessGate"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAgentsGovernance(this IServiceCollection services)
    {
        services.AddSingleton<IInvokerContextAccessor, InvokerContextAccessor>();
        services.AddSingleton<IConnectorAccessGate, InMemoryConnectorAccessGate>();
        return services;
    }
}
