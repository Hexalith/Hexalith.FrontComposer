using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

public sealed class ClaimsPrincipalUserContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontComposerAuthenticationOptions> options,
    ILogger<ClaimsPrincipalUserContextAccessor> logger) : IUserContextAccessor {
    // P7 — cache the extraction result for the lifetime of this scoped accessor. Reading
    // `TenantId` and `UserId` independently used to invoke `Read()` twice per render, doubling
    // claim enumeration AND duplicating HFC2012 warnings on failure.
    private FrontComposerClaimExtractionResult? _cached;
    private object? _principalKey;

    public string? TenantId => Read().TenantId;

    public string? UserId => Read().UserId;

    private FrontComposerClaimExtractionResult Read() {
        System.Security.Claims.ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
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
}
