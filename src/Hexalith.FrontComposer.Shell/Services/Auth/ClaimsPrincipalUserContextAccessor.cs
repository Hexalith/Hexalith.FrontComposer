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
    public string? TenantId => Read().TenantId;

    public string? UserId => Read().UserId;

    private FrontComposerClaimExtractionResult Read() {
        FrontComposerAuthenticationOptions current = options.Value;
        FrontComposerClaimExtractionResult result = FrontComposerClaimExtractor.Extract(
            httpContextAccessor.HttpContext?.User,
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

        return result;
    }
}
