using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

public sealed class AuthenticationStateUserContextAccessor(
    AuthenticationStateProvider authenticationStateProvider,
    IOptions<FrontComposerAuthenticationOptions> options,
    ILogger<AuthenticationStateUserContextAccessor> logger) : IUserContextAccessor {
    public string? TenantId => Read().TenantId;

    public string? UserId => Read().UserId;

    private FrontComposerClaimExtractionResult Read() {
        AuthenticationState state = authenticationStateProvider.GetAuthenticationStateAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
        FrontComposerAuthenticationOptions current = options.Value;
        FrontComposerClaimExtractionResult result = FrontComposerClaimExtractor.Extract(
            state.User,
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
