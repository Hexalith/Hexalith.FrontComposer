using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

public sealed class FrontComposerAuthRedirector(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontComposerAuthenticationOptions> options) : IAuthRedirector {
    public async Task RedirectAsync(string? returnUrl = null, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        HttpContext context = httpContextAccessor.HttpContext
            ?? throw new FrontComposerAuthenticationException(
                FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid,
                $"{FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid}: HTTP context is required for authentication challenge.");

        string sanitizedReturnUrl = FrontComposerReturnUrl.Sanitize(returnUrl);
        AuthenticationProperties properties = new() { RedirectUri = sanitizedReturnUrl };
        await context.ChallengeAsync(options.Value.SelectedChallengeScheme(), properties).ConfigureAwait(false);
    }
}
