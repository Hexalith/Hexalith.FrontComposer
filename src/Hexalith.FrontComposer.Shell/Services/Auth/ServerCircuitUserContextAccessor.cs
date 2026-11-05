using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Blazor Server <see cref="IUserContextAccessor"/> that resolves the signed-in principal from
/// <see cref="IHttpContextAccessor"/> during server-side render/prerender and from the circuit's
/// <see cref="AuthenticationStateProvider"/> (via <see cref="CircuitServicesAccessor"/>) during
/// interactive circuit activity, where <see cref="HttpContext"/> is <see langword="null"/>.
/// </summary>
/// <remarks>
/// Replaces the HttpContext-only <see cref="ClaimsPrincipalUserContextAccessor"/> in Blazor Server
/// hosts so interactive components (for example "My tenants", the Global Administrators page, and
/// value providers) observe the same signed-in user the header account menu and
/// <c>&lt;AuthorizeView&gt;</c> do. The fail-closed claim extraction is unchanged: a missing,
/// multi-valued, conflicting, or malformed tenant/user claim still yields a <see langword="null"/>
/// context (Decision D31). Only the principal-resolution seam differs, mirroring
/// <see cref="FrontComposerGatewayAuthorizationHandler"/>.
/// </remarks>
public sealed class ServerCircuitUserContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    IOptions<FrontComposerAuthenticationOptions> options,
    ILogger<ServerCircuitUserContextAccessor> logger) : IUserContextAccessor {
    // Cache the extraction result for the lifetime of this scoped accessor, keyed by the resolved
    // principal instance so a re-resolution (HttpContext -> circuit) re-runs extraction once.
    private FrontComposerClaimExtractionResult? _cached;
    private object? _principalKey;

    /// <inheritdoc />
    public string? TenantId => Read().TenantId;

    /// <inheritdoc />
    public string? UserId => Read().UserId;

    private FrontComposerClaimExtractionResult Read() {
        ClaimsPrincipal? principal = ResolvePrincipal();
        if (_cached is not null && ReferenceEquals(_principalKey, principal)) {
            return _cached;
        }

        FrontComposerAuthenticationOptions current = options.Value;
        FrontComposerClaimExtractionResult result = FrontComposerClaimExtractor.Extract(
            principal,
            [.. current.TenantClaimTypes],
            [.. current.UserClaimTypes]);
        if (!result.Succeeded) {
            FrontComposerSecurityLog.CircuitClaimExtractionFailed(
                logger,
                result.Reason,
                result.TenantAliases.Count,
                result.UserAliases.Count);
        }

        _cached = result;
        _principalKey = principal;
        return result;
    }

    // Use the same circuit-first authority as the credential resolver. A retained HttpContext may
    // belong to an earlier request and must not override an active circuit, including anonymous state.
    private ClaimsPrincipal? ResolvePrincipal()
    {
        if (circuitServicesAccessor.Services?.GetService<AuthenticationStateProvider>() is { } provider)
        {
            Task<AuthenticationState> stateTask = provider.GetAuthenticationStateAsync();
            return stateTask.IsCompletedSuccessfully ? stateTask.Result.User : null;
        }

        return httpContextAccessor.HttpContext?.User;
    }
}
