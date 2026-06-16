using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
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
            logger.LogWarning(
                "{DiagnosticId}: Auth claim extraction failed. Reason={Reason}, TenantClaimAliases={TenantClaimAliases}, UserClaimAliases={UserClaimAliases}.",
                FcDiagnosticIds.HFC2012_AuthenticationClaimExtractionFailed,
                result.Reason,
                string.Join("|", result.TenantAliases),
                string.Join("|", result.UserAliases));
        }

        _cached = result;
        _principalKey = principal;
        return result;
    }

    // Mirrors FrontComposerGatewayAuthorizationHandler.ResolveUserIdAsync: prefer the request's
    // HttpContext principal (server-side render / prerender); when absent (interactive circuit),
    // read the circuit's AuthenticationStateProvider published by FrontComposerCircuitServicesHandler.
    // Only a synchronously completed auth-state task is read to avoid sync-over-async on this sync
    // surface; in a connected circuit the state is resolved before component lifecycle runs.
    private ClaimsPrincipal? ResolvePrincipal() {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true) {
            return user;
        }

        if (circuitServicesAccessor.Services?.GetService<AuthenticationStateProvider>() is { } provider) {
            Task<AuthenticationState> stateTask = provider.GetAuthenticationStateAsync();
            if (stateTask.IsCompletedSuccessfully) {
                return stateTask.Result.User;
            }
        }

        return user;
    }
}
