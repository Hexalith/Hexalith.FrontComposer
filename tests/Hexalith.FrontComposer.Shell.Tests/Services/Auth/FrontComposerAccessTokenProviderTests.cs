using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Auth;

public sealed class FrontComposerAccessTokenProviderTests {
    [Fact]
    public async Task GetAccessTokenAsync_InvokesHostProviderPerOperation() {
        int calls = 0;
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.CustomBrokered.Enabled = true;
            options.TokenRelay.HostAccessTokenProvider = _ => {
                calls++;
                return ValueTask.FromResult<string?>("token-" + calls.ToString(System.Globalization.CultureInfo.InvariantCulture));
            };
        });

        string? first = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);
        string? second = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        first.ShouldBe("token-1");
        second.ShouldBe("token-2");
        calls.ShouldBe(2);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Fails_WhenGitHubOAuthHasNoBrokeredToken() {
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.GitHubOAuth.Enabled = true;
        });

        FrontComposerAuthenticationException ex = await Should.ThrowAsync<FrontComposerAuthenticationException>(
            () => sut.GetAccessTokenAsync(TestContext.Current.CancellationToken).AsTask());

        ex.DiagnosticId.ShouldBe(FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ObservesCancellationBeforeHostProvider() {
        bool called = false;
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        FrontComposerAccessTokenProvider sut = Build(options => {
            options.CustomBrokered.Enabled = true;
            options.TokenRelay.HostAccessTokenProvider = _ => {
                called = true;
                return ValueTask.FromResult<string?>("token");
            };
        });

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => sut.GetAccessTokenAsync(cts.Token).AsTask());

        called.ShouldBeFalse();
    }

    private static FrontComposerAccessTokenProvider Build(Action<FrontComposerAuthenticationOptions> configure) {
        FrontComposerAuthenticationOptions options = new();
        configure(options);
        return new FrontComposerAccessTokenProvider(
            new HttpContextAccessor(),
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<FrontComposerAccessTokenProvider>.Instance);
    }
}
