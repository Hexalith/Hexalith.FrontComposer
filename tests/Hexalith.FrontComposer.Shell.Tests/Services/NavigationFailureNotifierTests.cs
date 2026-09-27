using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Contracts.Registration;
using NSubstitute;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class NavigationFailureNotifierTests
{
    [Fact]
    public async Task ConcurrentActivationsOfTheSameRouteConfirmIndependently()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        Task<bool> first = notifier.PrepareRouteConfirmation("https://localhost/counter");
        Task<bool> second = notifier.PrepareRouteConfirmation("https://localhost/counter");

        first.IsCompleted.ShouldBeFalse();
        second.IsCompleted.ShouldBeFalse();
        notifier.ConfirmRoute("https://localhost/counter");

        (await first.ConfigureAwait(true)).ShouldBeTrue();
        (await second.ConfigureAwait(true)).ShouldBeTrue();
        notifier.Message.ShouldBeNull();
    }

    [Theory]
    [InlineData("tab=users")]
    [InlineData("tab=%75sers")]
    [InlineData("%74ab=users")]
    [InlineData("%74ab=%75sers")]
    public async Task QueryBackedModuleAliasConfirmsAfterCanonicalChildRenders(string query)
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        Task<bool> confirmation = notifier.PrepareRouteConfirmation($"https://localhost/tenants?{query}");

        notifier.ConfirmRoute("https://localhost/tenants/workspace-users");

        (await confirmation.ConfigureAwait(true)).ShouldBeTrue();
        notifier.LastConfirmedUri.ShouldBe("https://localhost/tenants/workspace-users");
    }

    [Fact]
    public void OpaqueTenantDetailDoesNotConfirmAWorkspaceActivation()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        Task<bool> confirmation = notifier.PrepareRouteConfirmation("https://localhost/tenants?tab=users");

        notifier.ConfirmRoute("https://localhost/tenants/customer-123");

        confirmation.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task NewDestinationSupersedesAnUnrenderedAttemptWithoutReportingFailure()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        Task<bool> superseded = notifier.PrepareRouteConfirmation("https://localhost/counter");
        Task<bool> current = notifier.PrepareRouteConfirmation("https://localhost/parties");

        (await superseded.ConfigureAwait(true)).ShouldBeFalse();
        notifier.Message.ShouldBeNull();
        notifier.ConfirmRoute("https://localhost/parties");
        (await current.ConfigureAwait(true)).ShouldBeTrue();
    }

    [Fact]
    public async Task BrowserNavigationRetiresPendingConfirmation()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        notifier.BeginAttempt("Counter", "https://localhost/counter");
        Task<bool> confirmation = notifier.PrepareRouteConfirmation("https://localhost/counter");

        notifier.ObserveLocation("https://localhost/home");

        (await confirmation.ConfigureAwait(true)).ShouldBeFalse();
        notifier.Message.ShouldBeNull();
        notifier.ReturnUriForActivatedUnmatched("https://localhost/counter").ShouldBeNull();
    }

    [Fact]
    public void UnrelatedUnmatchedRouteDoesNotOwnReturnNavigation()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        notifier.ConfirmRoute("https://localhost/parties");
        notifier.BeginAttempt("Missing", "https://localhost/missing");

        notifier.ReturnUriForActivatedUnmatched("https://localhost/missing")
            .ShouldBe("https://localhost/parties");
        notifier.ObserveLocation("https://localhost/unrelated");
        notifier.ReturnUriForActivatedUnmatched("https://localhost/unrelated").ShouldBeNull();
    }

    [Fact]
    public void RepeatedFailureHasDistinctAttemptIdentity()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        notifier.BeginAttempt("Missing", "https://localhost/missing");
        long first = notifier.AttemptVersion;
        notifier.ReportFailure();

        notifier.BeginAttempt("Missing", "https://localhost/missing");

        notifier.AttemptVersion.ShouldBe(first + 1);
        notifier.Message.ShouldBeNull();
    }

    [Fact]
    public async Task ScopeChangeRetiresPendingAttemptAndPreviousRoute()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        notifier.ConfirmRoute("https://localhost/tenants/workspace-users");
        notifier.BeginAttempt("Parties", "https://localhost/parties");
        Task<bool> pending = notifier.PrepareRouteConfirmation("https://localhost/parties");

        notifier.ResetForScopeChange();

        (await pending.ConfigureAwait(true)).ShouldBeFalse();
        notifier.LastConfirmedUri.ShouldBeNull();
        notifier.OwnsAttempt("https://localhost/parties").ShouldBeFalse();
        notifier.ReturnUriForActivatedUnmatched("https://localhost/parties").ShouldBeNull();
    }

    [Fact]
    public async Task HostWithoutFocusOwnerSettlesMatchingLocation()
    {
        NavigationFailureNotifier notifier = CreateNotifier();
        notifier.BeginAttempt("Counter", "https://localhost/counter");
        Task<bool> pending = notifier.PrepareRouteConfirmation("https://localhost/counter");
        notifier.ObserveLocation("https://localhost/counter");
        (await pending.ConfigureAwait(true)).ShouldBeTrue();
    }

    private static NavigationFailureNotifier CreateNotifier()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddLocalization();
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([new DomainManifest("Tenants", "tenants", [], []) {
            CanonicalRouteAliases = new Dictionary<string, string> { ["/tenants?tab=users"] = "/tenants/workspace-users" },
        }]);
        services.AddSingleton(registry);
        ServiceProvider provider = services.BuildServiceProvider();
        return new NavigationFailureNotifier(provider.GetRequiredService<IStringLocalizer<FcShellResources>>(), provider);
    }
}
