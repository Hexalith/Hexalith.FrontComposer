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

public sealed class ClaimsPrincipalUserContextAccessorTests {
    [Theory]
    [InlineData("FakeOidc", "tenant_id", "sub")]
    [InlineData("FakeSaml", "http://schemas.xmlsoap.org/claims/tenant", ClaimTypes.NameIdentifier)]
    [InlineData("FakeGitHubOAuth", "tenant_id", "id")]
    public void Accessor_ReadsConfiguredClaims_ForProviderShapes(string provider, string tenantClaim, string userClaim) {
        ClaimsPrincipal principal = Principal(
            new(tenantClaim, "  TenantA  "),
            new(userClaim, "  UserA  "));
        ClaimsPrincipalUserContextAccessor accessor = Build(principal, [tenantClaim], [userClaim], out _);

        accessor.TenantId.ShouldBe("TenantA", provider);
        accessor.UserId.ShouldBe("UserA", provider);
    }

    [Fact]
    public void Accessor_PreservesTenantCasing() {
        ClaimsPrincipalUserContextAccessor accessor = Build(
            Principal(new("tenant_id", "Acme_Corp"), new("sub", "Alice")),
            ["tenant_id"],
            ["sub"],
            out _);

        accessor.TenantId.ShouldBe("Acme_Corp");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tenant:evil")]
    public void Accessor_FailsClosed_WhenTenantClaimIsInvalid(string? value) {
        ClaimsPrincipalUserContextAccessor accessor = Build(
            Principal(new("tenant_id", value ?? string.Empty), new("sub", "alice")),
            ["tenant_id"],
            ["sub"],
            out CapturingLogger<ClaimsPrincipalUserContextAccessor> logger);

        accessor.TenantId.ShouldBeNull();
        accessor.UserId.ShouldBeNull();
        logger.Messages.ShouldContain(m => m.Contains(FcDiagnosticIds.HFC2012_AuthenticationClaimExtractionFailed, StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains("tenant:evil", StringComparison.Ordinal));
    }

    [Fact]
    public void Accessor_FailsClosed_WhenAliasValuesConflict() {
        ClaimsPrincipalUserContextAccessor accessor = Build(
            Principal(new("tenant_id", "tenant-a"), new("tid", "tenant-b"), new("sub", "alice")),
            ["tenant_id", "tid"],
            ["sub"],
            out CapturingLogger<ClaimsPrincipalUserContextAccessor> logger);

        accessor.TenantId.ShouldBeNull();
        accessor.UserId.ShouldBeNull();
        logger.Messages.ShouldContain(m => m.Contains("tenant_id", StringComparison.Ordinal) && m.Contains("tid", StringComparison.Ordinal));
        logger.Messages.ShouldNotContain(m => m.Contains("tenant-a", StringComparison.Ordinal) || m.Contains("tenant-b", StringComparison.Ordinal));
    }

    [Fact]
    public void Accessor_FailsClosed_WhenPrincipalIsUnauthenticated() {
        ClaimsPrincipal principal = new(new ClaimsIdentity());
        ClaimsPrincipalUserContextAccessor accessor = Build(principal, ["tenant_id"], ["sub"], out _);

        accessor.TenantId.ShouldBeNull();
        accessor.UserId.ShouldBeNull();
    }

    private static ClaimsPrincipalUserContextAccessor Build(
        ClaimsPrincipal principal,
        IReadOnlyList<string> tenantClaims,
        IReadOnlyList<string> userClaims,
        out CapturingLogger<ClaimsPrincipalUserContextAccessor> logger) {
        DefaultHttpContext context = new() { User = principal };
        HttpContextAccessor http = new() { HttpContext = context };
        FrontComposerAuthenticationOptions options = new();
        options.TenantClaimTypes.Clear();
        options.UserClaimTypes.Clear();
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
