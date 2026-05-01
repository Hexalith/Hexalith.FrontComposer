using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerAuthRedirectorTests {
    [Theory]
    [InlineData("/orders/1", "/orders/1")]
    [InlineData("~/orders/1", "/orders/1")]
    [InlineData("?tab=summary", "/?tab=summary")]
    [InlineData("/orders/1#details", "/orders/1#details")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    public void SanitizeReturnUrl_PreservesLocalReturnPaths(string? input, string expected)
        => FrontComposerReturnUrl.Sanitize(input).ShouldBe(expected);

    [Theory]
    [InlineData("https://evil.test/callback")]
    [InlineData("//evil.test/callback")]
    [InlineData("/\\evil")]
    [InlineData("/%5cevil")]
    [InlineData("/%250d%250aHeader:%20value")]
    [InlineData("/%2f%2fevil.test")]
    // P4 — additional unsafe shapes covered:
    [InlineData("javascript:alert(1)")]
    [InlineData("JaVaScRiPt:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("/@evil.com/path")]
    public void SanitizeReturnUrl_DropsUnsafeReturnPaths(string input)
        => FrontComposerReturnUrl.Sanitize(input).ShouldBe("/");

    /// <summary>P3/P4 — Unicode format characters. Codepoints passed as int and materialized
    /// inside the test body so the test source itself never embeds raw control characters.</summary>
    [Theory]
    [InlineData(0x00A0)] // NO-BREAK SPACE
    [InlineData(0x200B)] // ZERO WIDTH SPACE
    [InlineData(0x200C)] // ZERO WIDTH NON-JOINER
    [InlineData(0x200D)] // ZERO WIDTH JOINER
    [InlineData(0x200E)] // LEFT-TO-RIGHT MARK
    [InlineData(0x200F)] // RIGHT-TO-LEFT MARK
    [InlineData(0x2028)] // LINE SEPARATOR
    [InlineData(0x2029)] // PARAGRAPH SEPARATOR
    [InlineData(0x202E)] // RIGHT-TO-LEFT OVERRIDE
    [InlineData(0x2060)] // WORD JOINER
    [InlineData(0xFEFF)] // BOM
    public void SanitizeReturnUrl_RejectsForbiddenUnicodeFormatChars(int codepoint) {
        string input = "/foo" + (char)codepoint + "evil";

        FrontComposerReturnUrl.Sanitize(input).ShouldBe("/");
    }

    [Fact]
    public void SanitizeReturnUrl_DropsOversizeReturnPaths() {
        string oversize = "/" + new string('a', FrontComposerReturnUrl.MaxReturnUrlLength + 1);

        FrontComposerReturnUrl.Sanitize(oversize).ShouldBe("/");
    }

    [Fact]
    public async Task RedirectAsync_ThrowsHFC2011_WhenHttpContextIsAbsent() {
        FrontComposerAuthRedirector sut = new(
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(BuildOptions()));

        FrontComposerAuthenticationException ex = await Should.ThrowAsync<FrontComposerAuthenticationException>(
            () => sut.RedirectAsync("/safe", TestContext.Current.CancellationToken));

        ex.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2011_AuthenticationConfigurationInvalid);
    }

    [Fact]
    public async Task RedirectAsync_ObservesPreCallCancellation() {
        FrontComposerAuthRedirector sut = new(
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(BuildOptions()));
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => sut.RedirectAsync("/safe", cts.Token));
    }

    [Fact]
    public async Task RedirectAsync_InvokesChallengeWithSelectedScheme() {
        FrontComposerAuthenticationOptions options = BuildOptions();
        DefaultHttpContext context = BuildAuthenticatedContext(out FakeAuthenticationService authService);
        HttpContextAccessor http = new() { HttpContext = context };
        FrontComposerAuthRedirector sut = new(http, Microsoft.Extensions.Options.Options.Create(options));

        await sut.RedirectAsync("/safe", TestContext.Current.CancellationToken);

        authService.ChallengeCalls.Count.ShouldBe(1);
        authService.ChallengeCalls[0].Scheme.ShouldBe(options.SelectedChallengeScheme());
        authService.ChallengeCalls[0].RedirectUri.ShouldBe("/safe");
    }

    [Fact]
    public async Task RedirectAsync_DropsUnsafeReturnUrlBeforeChallenge() {
        FrontComposerAuthenticationOptions options = BuildOptions();
        DefaultHttpContext context = BuildAuthenticatedContext(out FakeAuthenticationService authService);
        HttpContextAccessor http = new() { HttpContext = context };
        FrontComposerAuthRedirector sut = new(http, Microsoft.Extensions.Options.Options.Create(options));

        await sut.RedirectAsync("https://evil.test/", TestContext.Current.CancellationToken);

        authService.ChallengeCalls[0].RedirectUri.ShouldBe("/");
    }

    private static FrontComposerAuthenticationOptions BuildOptions() {
        FrontComposerAuthenticationOptions options = new();
        options.CustomBrokered.Enabled = true;
        options.TokenRelay.HostAccessTokenProvider = _ => ValueTask.FromResult<string?>("token");
        options.TenantClaimTypes.Add("tenant_id");
        options.UserClaimTypes.Add("sub");
        return options;
    }

    private static DefaultHttpContext BuildAuthenticatedContext(out FakeAuthenticationService authService) {
        DefaultHttpContext context = new();
        ServiceCollection services = new();
        authService = new FakeAuthenticationService();
        services.AddSingleton<IAuthenticationService>(authService);
        context.RequestServices = services.BuildServiceProvider();
        return context;
    }

    private sealed class FakeAuthenticationService : IAuthenticationService {
        public List<(string? Scheme, string? RedirectUri)> ChallengeCalls { get; } = [];

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) {
            ChallengeCalls.Add((scheme, properties?.RedirectUri));
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    }
}
