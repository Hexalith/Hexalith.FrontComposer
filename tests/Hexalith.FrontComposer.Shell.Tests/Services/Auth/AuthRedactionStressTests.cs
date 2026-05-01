using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

/// <summary>
/// P16 — Redaction stress test. Story 7-1 AC13 forbids token, ID-token, refresh-token,
/// authorization-code, NameID, subject-identifier, email, display-name, raw claim, or tenant/user
/// ID values from appearing in logs or exception messages. This fixture injects high-temptation
/// values into claims and asserts they never reach `CapturingLogger.Messages` or
/// `Exception.Message` for any extraction failure path.
/// </summary>
public sealed class AuthRedactionStressTests {
    private const string JwtShaped = "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiJhbGljZSJ9.signature-bytes";
    private const string EmailShaped = "alice@evil.test";
    private const string DisplayNameShaped = "Alice O'Connor";
    private const string TenantShaped = "tenant-acme-corp-12345";

    public static IEnumerable<object[]> ForbiddenLeakValues => [
        [JwtShaped],
        [EmailShaped],
        [DisplayNameShaped],
        [TenantShaped],
    ];

    [Theory]
    [MemberData(nameof(ForbiddenLeakValues))]
    public void ColonClaim_FailureLogs_NeverContainClaimValue(string forbidden) {
        // Inject a colon to trigger the rejection branch with a JWT/email/display-shaped value.
        ClaimsPrincipalUserContextAccessor accessor = Build(
            principal: Principal(
                new("tenant_id", forbidden + ":colon"),
                new("sub", "alice")),
            tenantClaims: ["tenant_id"],
            userClaims: ["sub"],
            out CapturingLogger<ClaimsPrincipalUserContextAccessor> logger);

        accessor.TenantId.ShouldBeNull();
        logger.Messages.ShouldContain(m => m.Contains(FcDiagnosticIds.HFC2012_AuthenticationClaimExtractionFailed, StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains(forbidden, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ForbiddenLeakValues))]
    public void ConflictingAlias_FailureLogs_NeverContainClaimValue(string forbidden) {
        ClaimsPrincipalUserContextAccessor accessor = Build(
            principal: Principal(
                new("tenant_id", forbidden + "-a"),
                new("tid", forbidden + "-b"),
                new("sub", "alice")),
            tenantClaims: ["tenant_id", "tid"],
            userClaims: ["sub"],
            out CapturingLogger<ClaimsPrincipalUserContextAccessor> logger);

        accessor.TenantId.ShouldBeNull();
        logger.Messages.ShouldContain(m => m.Contains(FcDiagnosticIds.HFC2012_AuthenticationClaimExtractionFailed, StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains(forbidden + "-a", StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains(forbidden + "-b", StringComparison.Ordinal));
    }

    [Fact]
    public void TokenAcquisitionException_DoesNotIncludeJwtSubstring_InMessage() {
        // Adopter-supplied HostAccessTokenProvider throws with a JWT-shaped string in the
        // exception message; sanitized FrontComposerAuthenticationException must NOT echo it.
        InvalidOperationException root = new("inner failure embedding " + JwtShaped + " token body");
        FrontComposerAuthenticationOptions options = new();
        options.CustomBrokered.Enabled = true;
        options.TokenRelay.HostAccessTokenProvider = _ => throw root;
        options.TenantClaimTypes.Add("tenant_id");
        options.UserClaimTypes.Add("sub");

        FrontComposerAccessTokenProvider sut = new(
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(options),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<FrontComposerAccessTokenProvider>.Instance);

        FrontComposerAuthenticationException ex = Should.Throw<FrontComposerAuthenticationException>(
            () => sut.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask().GetAwaiter().GetResult());

        ex.Message.ShouldNotContain(JwtShaped);
        ex.Message.ShouldNotContain("inner failure"); // sanitized — operators read the inner exception, not the message
    }

    private static ClaimsPrincipalUserContextAccessor Build(
        ClaimsPrincipal principal,
        IReadOnlyList<string> tenantClaims,
        IReadOnlyList<string> userClaims,
        out CapturingLogger<ClaimsPrincipalUserContextAccessor> logger) {
        DefaultHttpContext context = new() { User = principal };
        HttpContextAccessor http = new() { HttpContext = context };
        FrontComposerAuthenticationOptions options = new();
        foreach (string claim in tenantClaims) {
            options.TenantClaimTypes.Add(claim);
        }

        foreach (string claim in userClaims) {
            options.UserClaimTypes.Add(claim);
        }

        logger = new();
        return new ClaimsPrincipalUserContextAccessor(http, Microsoft.Extensions.Options.Options.Create(options), logger);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "FakeOidc"));
}
