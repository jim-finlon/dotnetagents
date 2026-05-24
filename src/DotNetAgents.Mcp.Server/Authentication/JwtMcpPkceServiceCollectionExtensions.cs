// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetAgents.Mcp.Server.Authentication;

/// <summary>
/// Registration helpers for the per-service JWT issuer + validator pair. Hosting services call
/// <see cref="AddJwtMcpPkceTokens{TIssuer, TKeyProvider}"/> after <c>AddMcpAuthServer</c> and
/// pipe <c>UseMcpPkceBearer()</c> into the app pipeline before any /mcp gates.
/// </summary>
public static class JwtMcpPkceServiceCollectionExtensions
{
    /// <summary>
    /// Register a per-service issuer (replacing <see cref="DefaultMcpPkceTokenIssuer"/>),
    /// signing-key provider, validator, and validator options. Hosting code is expected to also
    /// configure <see cref="JwtMcpPkceTokenIssuerOptions"/> + <see cref="JwtMcpPkceBearerValidationOptions"/>
    /// either inline or via configuration sections.
    /// </summary>
    public static IServiceCollection AddJwtMcpPkceTokens<TIssuer, TKeyProvider>(
        this IServiceCollection services,
        Action<JwtMcpPkceTokenIssuerOptions>? configureIssuer = null,
        Action<JwtMcpPkceBearerValidationOptions>? configureValidator = null,
        IConfiguration? configuration = null)
        where TIssuer : JwtMcpPkceTokenIssuerBase
        where TKeyProvider : class, ISigningKeyProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<JwtMcpPkceTokenIssuerOptions>();
        services.AddOptions<JwtMcpPkceBearerValidationOptions>();
        if (configuration is not null)
        {
            services.Configure<JwtMcpPkceTokenIssuerOptions>(configuration.GetSection(JwtMcpPkceTokenIssuerOptions.SectionName));
            services.Configure<JwtMcpPkceBearerValidationOptions>(configuration.GetSection(JwtMcpPkceBearerValidationOptions.SectionName));
        }

        if (configureIssuer is not null)
        {
            services.PostConfigure(configureIssuer);
        }

        if (configureValidator is not null)
        {
            services.PostConfigure(configureValidator);
        }

        services.TryAddSingleton<ISigningKeyProvider, TKeyProvider>();
        services.RemoveAll<IMcpPkceTokenIssuer>();
        services.AddSingleton<IMcpPkceTokenIssuer, TIssuer>();
        services.TryAddSingleton<JwtMcpPkceBearerValidator>();
        return services;
    }
}
