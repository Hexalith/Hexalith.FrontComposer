using System.Reflection;
using System.Security.Claims;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Authenticated rendering of the header account control. The connected user's display name must
/// surface (as the avatar initials and the disabled menu item) even when the token does not carry
/// a claim that maps to <see cref="System.Security.Principal.IIdentity.Name"/> — the common Keycloak
/// case where only <c>preferred_username</c> / <c>given_name</c>+<c>family_name</c> are present.
/// </summary>
public sealed class FcAccountMenuAuthenticatedTests : LayoutComponentTestBase {
    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider {
        private Task<AuthenticationState> _state = Task.FromResult(new AuthenticationState(user));

        public Exception? SynchronousReadFailure { get; set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() {
            if (SynchronousReadFailure is not null) throw SynchronousReadFailure;
            return _state;
        }

        public void SetInitialState(Task<AuthenticationState> state) => _state = state;

        public void SetState(Task<AuthenticationState> state) {
            _state = state;
            NotifyAuthenticationStateChanged(state);
        }
    }

    /// <summary>Pending and obsolete notifications cannot retain or restore a stale account.</summary>
    [Fact]
    public async Task PendingAndOutOfOrderAuthenticationDoesNotExposeAnOldPrincipal() {
        ClaimsPrincipal alice = new(new ClaimsIdentity([new Claim("name", "Alice")], "oidc"));
        ClaimsPrincipal bob = new(new ClaimsIdentity([new Claim("name", "Bob")], "oidc"));
        AccountMenuAuthenticationStateProvider provider = new(alice);
        Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider>(_ => provider));
        EnsureStoreInitialized();
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Alice"));
        TaskCompletionSource<AuthenticationState> pending = new();
        await cut.InvokeAsync(() => provider.PublishState(pending.Task));
        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("Alice"));
        await cut.InvokeAsync(() => provider.PublishUser(bob));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Bob"));
        await cut.InvokeAsync(() => pending.SetResult(new AuthenticationState(alice)));
        await cut.InvokeAsync(() => Task.CompletedTask);
        cut.Markup.ShouldContain("Bob");
        cut.Markup.ShouldNotContain("Alice");
        cut.Instance.Dispose();
        int renders = cut.RenderCount;
        await cut.InvokeAsync(() => provider.PublishUser(alice));
        cut.RenderCount.ShouldBe(renders);
    }

    /// <summary>Provider failures and cancellation withdraw the previous account.</summary>
    /// <param name="cancelled">Whether the authentication task was cancelled.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnavailableAuthenticationRendersAnonymously(bool cancelled) {
        ClaimsPrincipal alice = new(new ClaimsIdentity([new Claim("name", "Alice")], "oidc"));
        AccountMenuAuthenticationStateProvider provider = new(alice);
        Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider>(_ => provider));
        EnsureStoreInitialized();
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        Task<AuthenticationState> unavailable = cancelled
            ? Task.FromCanceled<AuthenticationState>(new CancellationToken(canceled: true))
            : Task.FromException<AuthenticationState>(new InvalidOperationException("authentication unavailable"));
        await cut.InvokeAsync(() => provider.PublishState(unavailable));
        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("Alice");
            cut.FindAll("[data-testid='fc-account-sign-in']").Count.ShouldBe(1);
        });
    }

    /// <summary>A provider that throws before returning a task also fails closed.</summary>
    [Fact]
    public void SynchronousAuthenticationFailureRendersAnonymously() {
        AccountMenuAuthenticationStateProvider provider = new(new ClaimsPrincipal(new ClaimsIdentity())) {
            InitialFailure = new InvalidOperationException("authentication unavailable"),
        };
        Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider>(_ => provider));
        EnsureStoreInitialized();
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='fc-account-sign-in']").Count.ShouldBe(1));
    }

    private StubAuthenticationStateProvider UseAuthenticatedUser(params Claim[] claims) {
        // authenticationType non-null => IsAuthenticated == true; with no ClaimTypes.Name claim,
        // Identity.Name stays null so the test exercises the claim-fallback resolution.
        ClaimsPrincipal user = new(new ClaimsIdentity(claims, authenticationType: "oidc"));
        var provider = new StubAuthenticationStateProvider(user);
        Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider>(_ => provider));
        EnsureStoreInitialized();
        return provider;
    }

    /// <summary>Refreshes the rendered account after sign-out and a subsequent sign-in.</summary>
    [Fact]
    public async Task UpdatesTheRenderedAccountWhenAuthenticationChanges() {
        ClaimsPrincipal alice = new(new ClaimsIdentity([new Claim("name", "Alice")], "oidc"));
        var provider = new AccountMenuAuthenticationStateProvider(alice);
        Services.Replace(ServiceDescriptor.Scoped<AuthenticationStateProvider>(_ => provider));
        EnsureStoreInitialized();
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-account-user']").TextContent.ShouldContain("Alice"));

        await cut.InvokeAsync(() => provider.PublishUser(new ClaimsPrincipal(new ClaimsIdentity())));
        cut.WaitForAssertion(() => {
            cut.FindAll("[data-testid='fc-account-sign-out']").ShouldBeEmpty();
            cut.FindAll("[data-testid='fc-account-sign-in']").Count.ShouldBe(1);
            cut.Markup.ShouldNotContain("Alice");
        });

        ClaimsPrincipal bob = new(new ClaimsIdentity([new Claim("name", "Bob")], "oidc"));
        await cut.InvokeAsync(() => provider.PublishUser(bob));
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-account-user']").TextContent.ShouldContain("Bob"));
    }

    [Fact]
    public void RendersDisplayNameFromPreferredUsernameWhenIdentityNameMissing() {
        UseAuthenticatedUser(new Claim("preferred_username", "jdupont"));

        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();

        cut.WaitForAssertion(() => {
            // The user name no longer renders inline in the header bar — it lives solely in the
            // avatar menu's disabled item (regression guard for the 2026-06-24 header relocation).
            cut.Markup.ShouldNotContain("data-testid=\"fc-account-name\"");
            cut.Markup.ShouldContain("jdupont");
            // The disabled header item must carry the name (regression: it previously rendered the long
            // "Signed in as {name}" sentence, which was clipped at the viewport edge).
            cut.Find("[data-testid='fc-account-user']").TextContent.ShouldContain("jdupont");
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-out\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-account-sign-in\"");
        });
    }

    [Fact]
    public void RendersDisplayNameFromGivenAndFamilyNameWhenNoFriendlyClaim() {
        UseAuthenticatedUser(
            new Claim("given_name", "Jean"),
            new Claim("family_name", "Dupont"));

        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Jean Dupont"));
    }

    [Fact]
    public void PrefersFriendlyNameClaimOverPreferredUsername() {
        UseAuthenticatedUser(
            new Claim("name", "Jean Dupont"),
            new Claim("preferred_username", "jdupont"));

        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Jean Dupont"));
    }

    [Fact]
    public async Task WithdrawsAccountIdentityWhenAuthenticationChanges() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Original account"));

        await cut.InvokeAsync(() => provider.SetState(Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())))));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
            cut.Markup.ShouldNotContain("Original account");
            cut.Markup.ShouldNotContain("data-testid=\"fc-account-sign-out\"");
        });
    }

    [Fact]
    public async Task DelayedOlderAuthenticationCannotRestoreAccountAfterSignOut() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        var pending = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        await cut.InvokeAsync(() => provider.SetState(pending.Task));
        Task staleObservation = CurrentObservation(cut);
        await cut.InvokeAsync(() => provider.SetState(Task.FromResult(AnonymousState())));
        await CurrentObservation(cut).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        pending.SetResult(AuthenticatedState("Stale account"));
        await staleObservation.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
        cut.Markup.ShouldNotContain("Stale account");
        cut.Markup.ShouldNotContain("Original account");
    }

    [Fact]
    public async Task FailedAuthenticationWithdrawsPreviouslyDisplayedIdentity() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        await cut.InvokeAsync(() => provider.SetState(Task.FromException<AuthenticationState>(
            new InvalidOperationException("Synthetic authentication failure"))));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
            cut.Markup.ShouldNotContain("Original account");
        });
    }

    [Fact]
    public async Task PendingNotificationWithdrawsPreviousIdentityBeforeCompletion() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        var pending = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        await cut.InvokeAsync(() => provider.SetState(pending.Task));
        Task observation = CurrentObservation(cut);
        observation.IsCompleted.ShouldBeFalse();
        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("Original account");
            cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
        });
        pending.SetResult(AuthenticatedState("New account"));
        await observation.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.Markup.ShouldContain("New account");
    }

    [Fact]
    public async Task InitialReadCompletingAfterSignOutCannotRestoreStaleIdentity() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        var pending = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.SetInitialState(pending.Task);
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        Task initialObservation = CurrentObservation(cut);
        initialObservation.IsCompleted.ShouldBeFalse();
        await cut.InvokeAsync(() => provider.SetState(Task.FromResult(AnonymousState())));
        await CurrentObservation(cut).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        pending.SetResult(AuthenticatedState("Stale initial account"));
        await initialObservation.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        await cut.InvokeAsync(() => { });
        cut.Markup.ShouldNotContain("Stale initial account");
        cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
    }

    [Fact]
    public async Task SynchronousInitialProviderFailureFallsBackToAnonymous() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        provider.SynchronousReadFailure = new InvalidOperationException("Synthetic synchronous provider failure");
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        await CurrentObservation(cut).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        cut.Markup.ShouldNotContain("Original account");
        cut.Markup.ShouldContain("data-testid=\"fc-account-sign-in\"");
        Renderer.UnhandledException.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task NotificationCompletingAfterDisposalCannotRestoreIdentity() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        var pending = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        await cut.InvokeAsync(() => provider.SetState(pending.Task));
        Task observation = CurrentObservation(cut);
        await cut.InvokeAsync(cut.Instance.Dispose);
        pending.SetResult(AuthenticatedState("Late account"));
        await observation.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        typeof(FcAccountMenu).GetField("_user", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(cut.Instance).ShouldBeNull();
    }

    [Fact]
    public async Task UnexpectedNotificationFailureIsObservedByBlazor() {
        var provider = UseAuthenticatedUser(new Claim("name", "Original account"));
        IRenderedComponent<FcAccountMenu> cut = Render<FcAccountMenu>();
        Exception failure = (Exception)Activator.CreateInstance(typeof(OutOfMemoryException), "Synthetic fatal provider failure")!;
        await cut.InvokeAsync(() => provider.SetState(Task.FromException<AuthenticationState>(failure)));
        Exception observed = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        observed.ShouldBeSameAs(failure);
    }

    private static Task CurrentObservation(IRenderedComponent<FcAccountMenu> cut)
        => (Task)typeof(FcAccountMenu).GetField("_authenticationObservation", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(cut.Instance)!;

    private static AuthenticationState AnonymousState() => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static AuthenticationState AuthenticatedState(string name)
        => new(new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", name)], "oidc")));

}
