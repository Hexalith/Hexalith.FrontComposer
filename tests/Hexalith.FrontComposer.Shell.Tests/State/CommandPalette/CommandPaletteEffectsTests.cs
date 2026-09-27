// Story 3-4 Task 10.4b / 10.4c (D7 / D8 / D9 / D20 / D21 / D22 / D23 — AC3 / AC4 / AC5 / AC6).
#pragma warning disable CA2007 // ConfigureAwait — test code
#pragma warning disable xUnit1051 // CancellationToken — substitute storage does not honour the token
using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Registration;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.Shortcuts;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.CommandPalette;

public class CommandPaletteEffectsTests {
    private const string TestTenant = "tenant-a";
    private const string TestUser = "user-1";

    [Fact]
    public async Task ConfirmedNavigationSettlesAfterQueryAndResultsChange()
    {
        PaletteResult result = new(PaletteResultCategory.Recent, "Orders", "", "/orders", null, 1, false);
        IState<FrontComposerCommandPaletteState> palette = Substitute.For<IState<FrontComposerCommandPaletteState>>();
        TestNavigationManager navigation = new();
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [result], navigationManager: navigation, suppliedPaletteState: palette);
        navigation.OnNavigate = null;
        Task activation = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);
        palette.Value.Returns(palette.Value with { Query = "changed", Results = [] });
        services.GetRequiredService<NavigationFailureNotifier>().ConfirmRoute("https://localhost/orders");
        await activation;
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.Received(1).Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    [Theory]
    [InlineData("/orders/Cr%C3%A9er")]
    [InlineData("/orders/a%20b")]
    public async Task EncodedRouteConfirmationClosesAndRecordsRecent(string route)
    {
        PaletteResult result = new(PaletteResultCategory.Recent, "Orders", "", route, null, 1, false);
        TestNavigationManager navigation = new();
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [result], navigationManager: navigation);
        navigation.OnNavigate = null;
        Task activation = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);
        services.GetRequiredService<NavigationFailureNotifier>().ConfirmRoute(new Uri(new Uri("https://localhost"), route).AbsoluteUri);
        await activation;
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.Received(1).Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    [Fact]
    public async Task ResolveShortcutAliasQuery_AliasesMapToCanonicalShortcuts() {
        foreach (string alias in new[] { "?", "help", "keys", "kb", "shortcut", "shortcuts", "Help", "KB" }) {
            CommandPaletteEffects.ResolveShortcutAliasQuery(alias).ShouldBe("shortcuts");
        }

        CommandPaletteEffects.ResolveShortcutAliasQuery("counter").ShouldBe("counter");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task DebounceCancelsEarlierKeystroke() {
        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out _);

        Task first = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "co"), dispatcher);
        Task second = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c2", "cou"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await first;
        await second;

        // Only the second debounce completes (the first was cancelled and swallowed).
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteResultsComputedAction>());
    }

    [Fact]
    public async Task HandlePaletteClosed_CancelsInFlightQuery() {
        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out _);

        Task query = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "cou"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(50));
        await sut.HandlePaletteClosed(new PaletteClosedAction("c-close"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(200));
        await query;

        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteResultsComputedAction>());
    }

    [Fact]
    public async Task ShortcutsQuery_BypassesScorer_AndReturnsRegistrations() {
        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out IServiceProvider sp);

        var shortcuts = (ShortcutService)sp.GetRequiredService<IShortcutService>();
        shortcuts.Register("ctrl+k", "PaletteShortcutDescription", () => Task.CompletedTask);
        shortcuts.Register("g h", "HomeShortcutDescription", () => Task.CompletedTask);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "shortcuts"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        // P25 (Pass-6): assert presence-by-binding and use >= rather than == so a future change
        // that auto-registers shell defaults inside ShortcutService's ctor doesn't silently
        // invalidate the assertion. The shortcuts-bypass branch must surface ALL registrations as
        // Shortcut-category results.
        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Length >= 2
            && a.Results.All(r => r.Category == PaletteResultCategory.Shortcut)
            && a.Results.Any(r => r.DescriptionKey == "PaletteShortcutDescription")
            && a.Results.Any(r => r.DescriptionKey == "HomeShortcutDescription")));
    }

    [Theory]
    [InlineData("?")]
    [InlineData("help")]
    [InlineData("keys")]
    [InlineData("kb")]
    [InlineData("shortcut")]
    public async Task ShortcutAliases_AllProduceShortcutResults(string alias) {
        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out IServiceProvider sp);
        var shortcuts = (ShortcutService)sp.GetRequiredService<IShortcutService>();
        shortcuts.Register("ctrl+k", "PaletteShortcutDescription", () => Task.CompletedTask);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", alias), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Length >= 1
            && a.Results.All(r => r.Category == PaletteResultCategory.Shortcut)));
    }

    [Fact]
    public async Task ContextualBonus_AppliesToMatchingBoundedContext() {
        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out IServiceProvider sp,
            currentContext: "Counter",
            manifests: [
                new DomainManifest("Counter", "Counter", ["Counter.CounterProjection"], []),
                new DomainManifest("Commerce", "Commerce", ["Commerce.CounterProjection"], []),
            ]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "Counter"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(HasContextualBonus));
    }

    private static bool HasContextualBonus(PaletteResultsComputedAction action) {
        PaletteResult? counterRow = action.Results.FirstOrDefault(r => r.BoundedContext == "Counter");
        PaletteResult? commerceRow = action.Results.FirstOrDefault(r => r.BoundedContext == "Commerce");
        return counterRow is not null
            && commerceRow is not null
            && counterRow.Score - commerceRow.Score == 15;
    }

    [Fact]
    public async Task NoContextualBonus_WhenNavigationContextIsNull() {
        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out IServiceProvider sp,
            currentContext: null,
            manifests: [new DomainManifest("Counter", "Counter", ["Counter.CounterProjection"], [])]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "Counter"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.All(r => !r.IsInCurrentContext)));
    }

    [Fact]
    public async Task SyntheticKeyboardShortcutsCommandEntry_AppearsInDefaultOpen() {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _);

        await sut.HandlePaletteOpened(new PaletteOpenedAction("c1"), dispatcher);

        // P22 (Pass-6): assert the full record shape per AC6 / D23 / D32. The earlier predicate
        // only checked CommandTypeName; a silent shape regression (wrong category, missing
        // description key, mistyped score) would have slipped through.
        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Any(r =>
                r.CommandTypeName == CommandPaletteEffects.KeyboardShortcutsSentinel
                && r.Category == PaletteResultCategory.Command
                && r.BoundedContext == string.Empty
                && r.RouteUrl == null
                && r.Score == 1000
                && r.DescriptionKey == "KeyboardShortcutsCommandDescription")));
    }

    [Fact]
    public async Task HydrateDoesNotRePersist() {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider sp);
        IStorageService storage = sp.GetRequiredService<IStorageService>();

        await sut.HandlePaletteHydrated(new PaletteHydratedAction(["/x"]), dispatcher);

        await storage.DidNotReceiveWithAnyArgs().SetAsync<string[]>(default!, default!);
    }

    // P27 (Pass-6): D10 hydrate-time route safety filter — `HandleAppInitialized` filters
    // tampered URLs through `CommandRouteBuilder.IsInternalRoute` BEFORE dispatching
    // `PaletteHydratedAction`, and the dispatched payload contains only the surviving (safe)
    // entries. This regression-guards the rejected-count logging path AND the filter contract
    // itself; previously no chunk-2 test exercised it.
    [Theory]
    [InlineData("//evil.example/pwn")]                  // protocol-relative URL
    [InlineData("https://attacker.example/")]           // explicit external scheme
    [InlineData("javascript:alert(1)")]                 // javascript scheme
    [InlineData("/domain/commerce/submit-order-command")] // legacy command route is read-only compatibility, never advertised
    public async Task HandleAppInitialized_FiltersTamperedUrls_BeforeDispatchingHydrate(string tampered) {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider sp);
        IStorageService storage = sp.GetRequiredService<IStorageService>();
        storage.GetAsync<string[]>(Arg.Any<string>()).Returns(["/safe/route", tampered]);

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        // The hydrate dispatch (if any) must NOT include the tampered URL — only the safe entry survives.
        dispatcher.Received().Dispatch(ArgEx.Is<PaletteHydratedAction>(a =>
            !a.RecentRouteUrls.Contains(tampered)
            && a.RecentRouteUrls.Contains("/safe/route")));
    }

    [Fact]
    public async Task HandleAppInitialized_AllTamperedUrls_DispatchesNoHydrateAction() {
        // When 100 % of the stored payload fails the IsInternalRoute filter, no PaletteHydratedAction
        // dispatches at all — the reducer would have nothing to assign, and `PaletteHydratedCompletedAction`
        // closes the hydration state.
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider sp);
        IStorageService storage = sp.GetRequiredService<IStorageService>();
        storage.GetAsync<string[]>(Arg.Any<string>()).Returns(["//evil/x", "javascript:alert(1)"]);

        await sut.HandleAppInitialized(new AppInitializedAction("c1"), dispatcher);

        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteHydratedAction>());
        dispatcher.Received().Dispatch(Arg.Any<PaletteHydratedCompletedAction>());
    }

    [Fact]
    public async Task HandlePaletteResultActivated_SyntheticEntry_RefillsPaletteWithoutClosing() {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _,
            paletteResults: [new PaletteResult(PaletteResultCategory.Command, "Keyboard Shortcuts", "", null, CommandPaletteEffects.KeyboardShortcutsSentinel, 1000, false)]);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0), dispatcher);

        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteQueryChangedAction>(q => q.Query == "shortcuts"));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
    }

    [Fact]
    public async Task HandlePaletteResultActivated_Command_NavigatesToCanonicalGeneratedRoute() {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _,
            manifests: [new DomainManifest("Commerce", "Commerce", [], ["Commerce.SubmitOrderCommand"])],
            paletteResults: [new PaletteResult(PaletteResultCategory.Command, "SubmitOrder", "Commerce", null, "Commerce.SubmitOrderCommand", 100, false)]);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.Received(1).Dispatch(ArgEx.Is<RecentRouteVisitedAction>(r => r.Url == "/commands/Commerce/SubmitOrderCommand"));
    }

    [Fact]
    public async Task HandlePaletteResultActivated_WaitsForRenderedRouteBeforeClosingOrRecordingRecent() {
        PaletteResult result = new(PaletteResultCategory.Recent, "Orders", "", "/orders", null, 1, false);
        TestNavigationManager navigation = new();
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [result], navigationManager: navigation);
        navigation.OnNavigate = null;

        Task activation = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());

        services.GetRequiredService<NavigationFailureNotifier>().ConfirmRoute("https://localhost/orders");
        await activation;
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.Received(1).Dispatch(ArgEx.Is<RecentRouteVisitedAction>(visit => visit.Url == "/orders"));
    }

    [Fact]
    public async Task HandlePaletteResultActivated_CanonicalRedirectWaitsForTheFinalRenderedHeading() {
        PaletteResult result = new(PaletteResultCategory.Recent, "Tenants", "", "/tenants?tab=users", null, 1, false);
        TestNavigationManager navigation = new();
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [result], navigationManager: navigation,
            manifests: [new DomainManifest("Tenants", "tenants", [], []) { CanonicalRouteAliases = new Dictionary<string, string> { ["/tenants?tab=users"] = "/tenants/workspace-users" } }]);
        navigation.OnNavigate = null;

        Task activation = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);
        navigation.NavigateTo("/tenants/workspace-users", replace: true);
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        services.GetRequiredService<NavigationFailureNotifier>().ConfirmRoute(navigation.Uri);
        await activation;

        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.Received(1).Dispatch(ArgEx.Is<RecentRouteVisitedAction>(visit => visit.Url == "/tenants?tab=users"));
    }

    [Fact]
    public async Task HandlePaletteResultActivated_LongRouteAuthorizationWaitDoesNotFailEarly() {
        PaletteResult result = new(PaletteResultCategory.Recent, "Orders", "", "/orders", null, 1, false);
        TestNavigationManager navigation = new();
        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [result], navigationManager: navigation);
        navigation.OnNavigate = null;

        Task activation = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);
        time.Advance(TimeSpan.FromSeconds(6));
        activation.IsCompleted.ShouldBeFalse();
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteActivationFailedAction>());

        services.GetRequiredService<NavigationFailureNotifier>().ConfirmRoute("https://localhost/orders");
        await activation;
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
    }

    [Fact]
    public async Task HandlePaletteResultActivated_SecondClickedResultOwnsConfirmationWithoutFirstFailure() {
        PaletteResult firstResult = new(PaletteResultCategory.Recent, "Orders", "", "/orders", null, 1, false);
        PaletteResult secondResult = new(PaletteResultCategory.Recent, "Parties", "", "/parties", null, 1, false);
        TestNavigationManager navigation = new();
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [firstResult, secondResult], navigationManager: navigation);
        navigation.OnNavigate = null;

        Task first = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, firstResult, string.Empty), dispatcher);
        Task second = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(1, secondResult, string.Empty), dispatcher);
        await first;

        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteActivationFailedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        services.GetRequiredService<NavigationFailureNotifier>().ConfirmRoute("https://localhost/parties");
        await second;

        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.Received(1).Dispatch(ArgEx.Is<RecentRouteVisitedAction>(visit => visit.Url == "/parties"));
        dispatcher.DidNotReceive().Dispatch(ArgEx.Is<RecentRouteVisitedAction>(visit => visit.Url == "/orders"));
    }

    [Fact]
    public async Task HandlePaletteResultActivated_RenderFailurePreservesPaletteAndQuery() {
        PaletteResult result = new(PaletteResultCategory.Recent, "Orders", "", "/orders", null, 1, false);
        TestNavigationManager navigation = new();
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [result], navigationManager: navigation);
        navigation.OnNavigate = null;

        Task activation = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);
        services.GetRequiredService<NavigationFailureNotifier>().ReportFailure();
        await activation;
        dispatcher.Received(1).Dispatch(Arg.Any<PaletteActivationFailedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    [Fact]
    public async Task HandlePaletteResultActivated_CommandRechecksCurrentFullPageReachability() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest("Commerce", "Commerce", [], ["Commerce.SubmitOrderCommand"]) {
            FullPageCommands = [],
        });
        CommandPaletteEffects sut = BuildEffects(
            out _,
            out IDispatcher dispatcher,
            out _,
            registry: registry,
            paletteResults: [new PaletteResult(PaletteResultCategory.Command, "SubmitOrder", "Commerce", null, "Commerce.SubmitOrderCommand", 100, false)]);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0), dispatcher);

        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteActivationDeniedAction>(a => a.Kind == PaletteActivationDenialKind.Unavailable));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    [Fact]
    public async Task HandlePaletteResultActivated_CommandWithNoCurrentOwningManifest_DeniesWithoutNavigation() {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _,
            manifests: [],
            paletteResults: [new PaletteResult(PaletteResultCategory.Command, "SubmitOrder", "Commerce", null, "Commerce.SubmitOrderCommand", 100, false)]);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0), dispatcher);

        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteActivationDeniedAction>(a => a.Kind == PaletteActivationDenialKind.Unavailable));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    [Theory]
    [InlineData("closed")]
    [InlineData("query")]
    [InlineData("selection")]
    [InlineData("scope")]
    public async Task HandlePaletteResultActivated_AuthorizationCompletesAfterPaletteChanges_DiscardsStaleActivation(string change) {
        string command = typeof(PaletteProtectedCommand).FullName!;
        PaletteResult result = new(PaletteResultCategory.Command, "Protected command", "Orders", null, command, 1, false);
        PaletteResult other = new(PaletteResultCategory.Recent, "Other", "", "/other", null, 1, false);
        TaskCompletionSource<CommandAuthorizationDecision> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => { entered.TrySetResult(); return held.Task; });
        ServiceCollection services = [];
        services.AddSingleton(CreateRegistry([new DomainManifest("Orders", "Orders", [], [command],
            CommandPolicies: new Dictionary<string, string> { [command] = "Approve" })]));
        services.AddSingleton(evaluator);
        services.AddSingleton<NavigationManager>(new TestNavigationManager());
        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerCommandPaletteState current = new(true, "protected", [result, other], [], 0, PaletteLoadState.Idle);
        IState<FrontComposerCommandPaletteState> palette = Substitute.For<IState<FrontComposerCommandPaletteState>>();
        palette.Value.Returns(_ => current);
        IState<FrontComposerNavigationState> navigation = Substitute.For<IState<FrontComposerNavigationState>>();
        navigation.Value.Returns(new FrontComposerNavigationState(false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        using CommandPaletteEffects sut = new(navigation, palette, EnabledLoggerSubstitute.Create<CommandPaletteEffects>(), provider);

        Task activation = sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, "protected"), dispatcher);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        current = change switch {
            "closed" => current with { IsOpen = false },
            "query" => current with { Query = "different" },
            "selection" => current with { SelectedIndex = 1 },
            "scope" => CommandPaletteReducers.ReduceScopeChanged(current, new ScopeChangedAction()),
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };
        held.SetResult(CommandAuthorizationDecision.Allowed("approved"));
        await activation;

        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteActivationDeniedAction>());
    }

    [Fact]
    public async Task HandlePaletteResultActivated_ReplacedResult_KeepsPaletteOpenAndQueryEditable() {
        PaletteResult stale = new(PaletteResultCategory.Recent, "Old page", "", "/old", null, 1, false);
        PaletteResult current = new(PaletteResultCategory.Recent, "Current page", "", "/current", null, 1, false);
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _, paletteResults: [current]);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, stale, string.Empty), dispatcher);

        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteActivationDeniedAction>(a => a.Kind == PaletteActivationDenialKind.Unavailable));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
        FrontComposerCommandPaletteState initial = new(true, "old", [current], [], 0, PaletteLoadState.Idle);
        FrontComposerCommandPaletteState denied = CommandPaletteReducers.ReduceActivationDenied(initial, new PaletteActivationDeniedAction(PaletteActivationDenialKind.Unavailable));
        FrontComposerCommandPaletteState edited = CommandPaletteReducers.ReducePaletteQueryChanged(denied, new PaletteQueryChangedAction("next", "new"));
        denied.IsOpen.ShouldBeTrue();
        edited.Query.ShouldBe("new");
    }

    [Fact]
    public async Task HandlePaletteResultActivated_PermissionRevoked_KeepsPaletteOpen() {
        string command = typeof(PaletteProtectedCommand).FullName!;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Denied, "denied")));
        PaletteResult result = new(PaletteResultCategory.Command, "Protected command", "Orders", null, command, 1, false);
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _,
            manifests: [new DomainManifest("Orders", "Orders", [], [command],
                CommandPolicies: new Dictionary<string, string> { [command] = "Approve" })],
            paletteResults: [result], authorizationEvaluator: evaluator);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);

        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteActivationDeniedAction>(a => a.Kind == PaletteActivationDenialKind.Permission));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    [Fact]
    public async Task HandlePaletteResultActivated_RemovedRecentCommandRoute_ReportsOneSafeFailureAndRetainsQuery() {
        PaletteResult result = new(PaletteResultCategory.Recent, "/commands/Counter/RemovedCommand?secret=opaque-token", "Counter",
            "/commands/Counter/RemovedCommand?secret=opaque-token", null, 1, false);
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            manifests: [new DomainManifest("Counter", "Counter", [], [])], paletteResults: [result]);
        NavigationFailureNotifier failure = services.GetRequiredService<NavigationFailureNotifier>();
        failure.RememberSuccessfulRoute("Counter");

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Any<PaletteActivationFailedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
        string message = failure.Message.ShouldNotBeNull();
        message.ShouldContain("that page");
        message.ShouldNotContain("Counter");
        message.ShouldNotContain("opaque-token");
        FrontComposerCommandPaletteState initial = new(true, "archived", [result], [], 0, PaletteLoadState.Idle);
        FrontComposerCommandPaletteState failed = CommandPaletteReducers.ReduceActivationFailed(initial, new PaletteActivationFailedAction());
        failed.IsOpen.ShouldBeTrue();
        failed.Query.ShouldBe("archived");
    }

    [Fact]
    public async Task HandlePaletteResultActivated_NavigateToThrows_ReportsSafeDestinationAndCurrentPage() {
        PaletteResult result = new(PaletteResultCategory.Projection, "Counter projection", "Counter",
            "/counter/counter-projection", null, 1, false);
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out IServiceProvider services,
            paletteResults: [result], navigationManager: new ThrowingNavigationManager());
        NavigationFailureNotifier failure = services.GetRequiredService<NavigationFailureNotifier>();
        failure.RememberSuccessfulRoute("Counter");

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0, result, string.Empty), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Any<PaletteActivationFailedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        string message = failure.Message.ShouldNotBeNull();
        message.ShouldContain("Could not open that page");
        message.ShouldNotContain("Counter projection");
        message.ShouldNotContain("Counter");
        message.ShouldNotContain("secret-token-opaque");
        message.ShouldNotContain("/counter/");
    }

    [Fact]
    public async Task HandlePaletteResultActivated_InformationalShortcut_DoesNotCloseOrNavigate() {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _,
            paletteResults: [new PaletteResult(PaletteResultCategory.Shortcut, "Ctrl+K", "", null, null, 0, false, null, "PaletteShortcutDescription")]);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0), dispatcher);

        dispatcher.DidNotReceive().Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    // P28 (Pass-6): Shortcut-category row with a non-empty RouteUrl (e.g., `g h → "/"`) must
    // navigate, close the palette, AND NOT record a Recent-route entry per AC6 / D11. The
    // pre-existing InformationalShortcut test only covered the RouteUrl=null path; this test
    // covers the with-route path so the `result.Category != Shortcut` recent-route guard cannot
    // silently regress.
    [Fact]
    public async Task HandlePaletteResultActivated_ShortcutRowWithRouteUrl_NavigatesAndClosesAndDoesNotPersistRecent() {
        CommandPaletteEffects sut = BuildEffects(out _, out IDispatcher dispatcher, out _,
            paletteResults: [new PaletteResult(PaletteResultCategory.Shortcut, "g h", "", "/", null, 0, false, null, "HomeShortcutDescription")]);

        await sut.HandlePaletteResultActivated(new PaletteResultActivatedAction(0), dispatcher);

        dispatcher.Received(1).Dispatch(Arg.Any<PaletteClosedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<RecentRouteVisitedAction>());
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_BlankQueryRestoresDefaultResults() {
        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            manifests: [new DomainManifest("Orders", "Orders", ["Orders.Domain.Projections.OrderLineItemView"], [])],
            recentRoutes: ["/orders/recent"]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", string.Empty), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Any(r => r.CommandTypeName == CommandPaletteEffects.KeyboardShortcutsSentinel)
            && a.Results.Any(r => r.Category == PaletteResultCategory.Recent && r.RouteUrl == "/orders/recent")
            && a.Results.Any(r => r.Category == PaletteResultCategory.Projection && r.RouteUrl == "/orders/order-line-item-view")));
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_ProjectionRoutesUseNavigationConvention() {
        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            manifests: [new DomainManifest("Orders", "Orders", ["Orders.Domain.Projections.OrderLineItemView"], [])]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "LineItem"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Any(r => r.RouteUrl == "/orders/order-line-item-view")));
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_FiltersNegativeScores() {
        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            manifests: [new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CxxxxxxxxxxxxxxxxxxxxxxxxV"], [])]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "cv"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a => a.Results.IsEmpty));
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_TrimsNonAliasWhitespaceBeforeScoring() {
        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            manifests: [new DomainManifest("Orders", "Orders", ["Orders.Domain.Projections.OrderLineItemView"], [])]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "  LineItem  "), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Any(r => r.RouteUrl == "/orders/order-line-item-view")));
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_FiltersUnreachableCommands_ViaHasFullPageRoute() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry, IFrontComposerFullPageRouteRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [], ["Counter.UnreachableCommand"]),
        ]);
        ((IFrontComposerFullPageRouteRegistry)registry).HasFullPageRoute("Counter.UnreachableCommand").Returns(false);

        CommandPaletteEffects sut = BuildEffects(out FakeTimeProvider time, out IDispatcher dispatcher, out _,
            registry: registry);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "Unreachable"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            !a.Results.Any(r => r.CommandTypeName == "Counter.UnreachableCommand")));
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_GeneratedMembership_SurfacesOnlyFullPageCommands() {
        FrontComposerRegistry registry = new([], [], NullLogger<FrontComposerRegistry>.Instance);
        registry.RegisterDomain(new DomainManifest(
            "Counter",
            "Counter",
            [],
            ["Counter.IncrementCommand", "Counter.ConfigureCounterCommand"]) {
            FullPageCommands = ["Counter.ConfigureCounterCommand"],
        });
        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            registry: registry);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "Command"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Any(r => r.CommandTypeName == "Counter.ConfigureCounterCommand")
            && !a.Results.Any(r => r.CommandTypeName == "Counter.IncrementCommand")));
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_FiltersDeniedProtectedCommands() {
        string commandTypeName = typeof(PaletteProtectedCommand).FullName!;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Denied, "corr-1"));

        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            manifests: [
                new DomainManifest(
                    "Orders",
                    "Orders",
                    [],
                    [commandTypeName],
                    CommandPolicies: new Dictionary<string, string>(StringComparer.Ordinal) {
                        [commandTypeName] = "OrderApprover",
                    }),
            ],
            authorizationEvaluator: evaluator);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "Protected"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received().Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            !a.Results.Any(r => r.CommandTypeName == commandTypeName)));
        await evaluator.Received(1).EvaluateAsync(
            ArgEx.Is<CommandAuthorizationRequest>(r =>
                r.PolicyName == "OrderApprover"
                && r.SourceSurface == CommandAuthorizationSurface.CommandPalette
                && r.BoundedContext == "Orders"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SameQuery_AuthorizationCompletesAfterScopeSwitch_OldResultsAreRejected() {
        string commandTypeName = typeof(PaletteProtectedCommand).FullName!;
        TaskCompletionSource<CommandAuthorizationDecision> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int evaluations = 0;
        ICommandAuthorizationEvaluator evaluator = Substitute.For<ICommandAuthorizationEvaluator>();
        evaluator.EvaluateAsync(Arg.Any<CommandAuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                if (Interlocked.Increment(ref evaluations) == 1) {
                    entered.TrySetResult();
                    return held.Task;
                }

                return Task.FromResult(CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Denied, "b"));
            });
        FakeTimeProvider time = new();
        ServiceCollection services = [];
        services.AddSingleton<TimeProvider>(time);
        services.AddSingleton(CreateRegistry([
            new DomainManifest("Orders", "Orders", [], [commandTypeName],
                CommandPolicies: new Dictionary<string, string>(StringComparer.Ordinal) {
                    [commandTypeName] = "OrderApprover",
                }),
        ]));
        services.AddSingleton(evaluator);
        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerCommandPaletteState current = new(true, "Protected", [], [], 0, PaletteLoadState.Searching);
        IState<FrontComposerCommandPaletteState> palette = Substitute.For<IState<FrontComposerCommandPaletteState>>();
        palette.Value.Returns(_ => current);
        IState<FrontComposerNavigationState> navigation = Substitute.For<IState<FrontComposerNavigationState>>();
        navigation.Value.Returns(new FrontComposerNavigationState(false, ImmutableDictionary<string, bool>.Empty, ViewportTier.Desktop));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        dispatcher.When(d => d.Dispatch(Arg.Any<PaletteResultsComputedAction>())).Do(call =>
            current = CommandPaletteReducers.ReducePaletteResultsComputed(current, call.Arg<PaletteResultsComputedAction>()));
        using CommandPaletteEffects effect = new(navigation, palette,
            EnabledLoggerSubstitute.Create<CommandPaletteEffects>(), provider);

        Task oldQuery = effect.HandlePaletteQueryChanged(new PaletteQueryChangedAction("a", "Protected"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        current = CommandPaletteReducers.ReduceScopeChanged(current, new ScopeChangedAction());
        current = CommandPaletteReducers.ReducePaletteOpened(current, new PaletteOpenedAction("b"));
        current = CommandPaletteReducers.ReducePaletteQueryChanged(current, new PaletteQueryChangedAction("b", "Protected"));

        Task newQuery = effect.HandlePaletteQueryChanged(new PaletteQueryChangedAction("b", "Protected"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await newQuery;
        current.Results.ShouldBeEmpty();
        held.SetResult(CommandAuthorizationDecision.Allowed("a"));
        await oldQuery;
        current.Results.ShouldBeEmpty();
        current.ScopeGeneration.ShouldBe(1);
    }

    // Story 2.7 Task 2 (AC2) — the durable DEFAULT-LANE integration pin: drive live registry
    // filtering end-to-end through the REAL PaletteScorer + effect across a MULTI-manifest registry
    // and assert the dispatched results surface EXACTLY the matching projections, ranked by score
    // (non-increasing), with a non-matching projection excluded. Mirrors the e2e-palette lane's AC2
    // essence (which is EXCLUDED from the default blocking lane) so AC2's "live registry search
    // surfaces matching projections" gets default-lane coverage.
    [Fact]
    public async Task HandlePaletteQueryChanged_MultiManifest_SurfacesMatchingProjectionsRanked() {
        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            manifests: [
                new DomainManifest(
                    "Orders",
                    "Orders",
                    ["Orders.Domain.Projections.OrderLineItemView", "Orders.Domain.Projections.OrderSummaryView"],
                    []),
                new DomainManifest("Billing", "Billing", ["Billing.Domain.Projections.InvoiceView"], []),
            ]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "Order"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a =>
            a.Results.Any(r => r.Category == PaletteResultCategory.Projection && r.RouteUrl == "/orders/order-line-item-view")
            && a.Results.Any(r => r.Category == PaletteResultCategory.Projection && r.RouteUrl == "/orders/order-summary-view")
            && !a.Results.Any(r => r.RouteUrl == "/billing/invoice-view")
            && IsRankedByScoreDescending(a)));
    }

    [Fact]
    public async Task HandlePaletteQueryChanged_NonMatchingQuery_SurfacesEmptyState() {
        CommandPaletteEffects sut = BuildEffects(
            out FakeTimeProvider time,
            out IDispatcher dispatcher,
            out _,
            manifests: [new DomainManifest("Orders", "Orders", ["Orders.Domain.Projections.OrderLineItemView"], [])]);

        Task pending = sut.HandlePaletteQueryChanged(new PaletteQueryChangedAction("c1", "zzzznomatch"), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        dispatcher.Received(1).Dispatch(ArgEx.Is<PaletteResultsComputedAction>(a => a.Results.IsEmpty));
    }

    private static bool IsRankedByScoreDescending(PaletteResultsComputedAction action) {
        for (int i = 1; i < action.Results.Length; i++) {
            if (action.Results[i - 1].Score < action.Results[i].Score) {
                return false;
            }
        }

        return true;
    }

    private static CommandPaletteEffects BuildEffects(
        out FakeTimeProvider time,
        out IDispatcher dispatcher,
        out IServiceProvider serviceProvider,
        IFrontComposerRegistry? registry = null,
        IReadOnlyList<DomainManifest>? manifests = null,
        IReadOnlyList<PaletteResult>? paletteResults = null,
        IReadOnlyList<string>? recentRoutes = null,
        string? currentContext = null,
        ICommandAuthorizationEvaluator? authorizationEvaluator = null,
        NavigationManager? navigationManager = null,
        IState<FrontComposerCommandPaletteState>? suppliedPaletteState = null) {
        time = new FakeTimeProvider();
        dispatcher = Substitute.For<IDispatcher>();

        IServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddHexalithShellLocalization();
        services.AddSingleton<NavigationFailureNotifier>();

        IFrontComposerRegistry effectiveRegistry = registry ?? CreateRegistry(manifests);
        services.AddSingleton(effectiveRegistry);

        FakeTimeProvider tp = time;
        services.AddSingleton<TimeProvider>(tp);
        services.AddSingleton<IShortcutService>(_ => new ShortcutService(tp, EnabledLoggerSubstitute.Create<ShortcutService>()));

        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(TestTenant);
        accessor.UserId.Returns(TestUser);
        services.AddSingleton(accessor);

        IStorageService storage = Substitute.For<IStorageService>();
        services.AddSingleton(storage);

        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(_ => Guid.NewGuid().ToString("N"));
        services.AddSingleton(ulids);

        if (authorizationEvaluator is not null) {
            services.AddSingleton(authorizationEvaluator);
        }

        // P6 (2026-04-21 pass-3) — register a test NavigationManager so activation happy-path tests
        // can observe the expected RecentRouteVisitedAction dispatch. The new effect contract logs
        // and early-returns (no RecentRouteVisited) when NavigationManager is unresolvable; that
        // failure path is covered by its own test.
        NavigationManager effectiveNavigation = navigationManager ?? new TestNavigationManager();
        services.AddSingleton(effectiveNavigation);

        serviceProvider = services.BuildServiceProvider();
        if (effectiveNavigation is TestNavigationManager testNavigation) {
            NavigationFailureNotifier confirmation = serviceProvider.GetRequiredService<NavigationFailureNotifier>();
            testNavigation.OnNavigate = uri => confirmation.ConfirmRoute(uri);
        }

        IState<FrontComposerCommandPaletteState> paletteState = suppliedPaletteState ?? Substitute.For<IState<FrontComposerCommandPaletteState>>();
        ImmutableArray<PaletteResult> results = paletteResults is null
            ? ImmutableArray<PaletteResult>.Empty
            : [.. paletteResults];
        ImmutableArray<string> recent = recentRoutes is null
            ? ImmutableArray<string>.Empty
            : [.. recentRoutes];
        paletteState.Value.Returns(new FrontComposerCommandPaletteState(
            IsOpen: true,
            Query: string.Empty,
            Results: results,
            RecentRouteUrls: recent,
            SelectedIndex: 0,
            LoadState: PaletteLoadState.Idle));

        IState<FrontComposerNavigationState> navState = Substitute.For<IState<FrontComposerNavigationState>>();
        navState.Value.Returns(new FrontComposerNavigationState(
            SidebarCollapsed: false,
            CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
            CurrentViewport: ViewportTier.Desktop,
            CurrentBoundedContext: currentContext));

        return new CommandPaletteEffects(
            navState,
            paletteState,
            EnabledLoggerSubstitute.Create<CommandPaletteEffects>(),
            serviceProvider);
    }

    private static IFrontComposerRegistry CreateRegistry(IReadOnlyList<DomainManifest>? manifests) {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry, IFrontComposerFullPageRouteRegistry>();
        registry.GetManifests().Returns(manifests ?? []);
        ((IFrontComposerFullPageRouteRegistry)registry).HasFullPageRoute(Arg.Any<string>()).Returns(true);
        return registry;
    }

    private sealed class TestNavigationManager : NavigationManager {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        public Action<string>? OnNavigate { get; set; }

        protected override void NavigateToCore(string uri, bool forceLoad) {
            Uri = ToAbsoluteUri(uri).ToString();
            OnNavigate?.Invoke(Uri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
            => NavigateToCore(uri, options.ForceLoad);
    }

    private sealed class ThrowingNavigationManager : NavigationManager {
        public ThrowingNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad)
            => throw new InvalidOperationException("secret-token-opaque");
    }
}

public sealed class PaletteProtectedCommand { }
