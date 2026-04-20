// ATDD RED PHASE — Story 3-2 Task 10.4 (D5, D6; AC3, AC4, AC5)
// Fails at compile until Task 5 (FcLayoutBreakpointWatcher component) lands.

using Bunit;
using Bunit.TestDoubles;

using Fluxor;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-2 Task 10.4 — watcher lifecycle tests.
/// D5 (headless + import on first render + DisposeAsync), D6 (JS module dedup + composed tier).
/// </summary>
public sealed class FcLayoutBreakpointWatcherTests : LayoutComponentTestBase {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-layout-breakpoints.js";

    public FcLayoutBreakpointWatcherTests() {
        EnsureStoreInitialized();
    }

    [Fact]
    public void ImportsModuleOnFirstRender() {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupModule("subscribe", _ => true);
        _ = module.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        IRenderedComponent<FcLayoutBreakpointWatcher> cut = Render<FcLayoutBreakpointWatcher>();

        cut.WaitForAssertion(() =>
            JSInterop.Invocations["import"].ShouldNotBeEmpty("Module must be imported on first render (D5)"));
    }

    [Fact]
    public async Task DispatchesInitialTierOnSubscribe() {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupModule("subscribe", _ => true);
        _ = module.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        IRenderedComponent<FcLayoutBreakpointWatcher> cut = Render<FcLayoutBreakpointWatcher>();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();

        // Simulate the JS module's initial subscribe emission: "browser is currently at Tablet".
        await cut.Instance.OnViewportTierChangedAsync((int)ViewportTier.Tablet);

        cut.WaitForAssertion(() =>
            state.Value.CurrentViewport.ShouldBe(ViewportTier.Tablet));
    }

    [Fact]
    public async Task DispatchesOnSubsequentChange() {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupModule("subscribe", _ => true);
        _ = module.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        IRenderedComponent<FcLayoutBreakpointWatcher> cut = Render<FcLayoutBreakpointWatcher>();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();

        await cut.Instance.OnViewportTierChangedAsync((int)ViewportTier.Desktop);
        state.Value.CurrentViewport.ShouldBe(ViewportTier.Desktop);

        await cut.Instance.OnViewportTierChangedAsync((int)ViewportTier.Phone);
        cut.WaitForAssertion(() => state.Value.CurrentViewport.ShouldBe(ViewportTier.Phone));
    }

    [Fact]
    public async Task DuplicateTierDoesNotRenotifyState() {
        // F9 — renamed from "DedupesWhenComposedTierUnchanged". This test asserts the downstream
        // invariant (same-tier dispatch produces no StateChanged via Fluxor's value-equality
        // short-circuit), NOT the dedup-at-the-JS-boundary contract in D6. The JS-side dedup
        // requires a separate JS unit test (tracked as a follow-up — no Vitest/Jest framework
        // is currently configured for the .js modules).
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupModule("subscribe", _ => true);
        _ = module.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        IRenderedComponent<FcLayoutBreakpointWatcher> cut = Render<FcLayoutBreakpointWatcher>();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();

        int changeCount = 0;
        state.StateChanged += (_, _) => Interlocked.Increment(ref changeCount);

        await cut.Instance.OnViewportTierChangedAsync((int)ViewportTier.CompactDesktop);
        int afterFirst = Volatile.Read(ref changeCount);
        await cut.Instance.OnViewportTierChangedAsync((int)ViewportTier.CompactDesktop);
        int afterSecond = Volatile.Read(ref changeCount);

        afterSecond.ShouldBe(afterFirst,
            "Dispatching the same tier twice must not re-notify subscribers (Fluxor value-equality short-circuit).");
    }

    [Fact]
    public async Task DispatchesCorrectTierOnDoubleBoundarySkip() {
        // Story 3-2 party-mode finding (Dr. Quinn) — fast resize from Desktop (>1366) to Tablet
        // (<1024) produces a single OnViewportTierChangedAsync(1) call (composed int ladder
        // collapses both crossings). No intermediate CompactDesktop action should fire.
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupModule("subscribe", _ => true);
        _ = module.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        IRenderedComponent<FcLayoutBreakpointWatcher> cut = Render<FcLayoutBreakpointWatcher>();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();

        // Start at Desktop (feature default), then jump directly to Tablet.
        state.Value.CurrentViewport.ShouldBe(ViewportTier.Desktop);
        await cut.Instance.OnViewportTierChangedAsync((int)ViewportTier.Tablet);

        cut.WaitForAssertion(() => {
            state.Value.CurrentViewport.ShouldBe(ViewportTier.Tablet,
                "Skip-tier jump must land on the reported tier, not the intermediate CompactDesktop.");
        });
    }

    [Fact]
    public async Task InvalidTierIsIgnored() {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupModule("subscribe", _ => true);
        _ = module.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        IRenderedComponent<FcLayoutBreakpointWatcher> cut = Render<FcLayoutBreakpointWatcher>();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();

        await cut.Instance.OnViewportTierChangedAsync(999);

        state.Value.CurrentViewport.ShouldBe(ViewportTier.Desktop,
            "Unknown breakpoint tiers must be ignored so the feature-default Desktop state remains intact.");
    }

    [Fact]
    public async Task DisposesCleanly() {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupModule("subscribe", _ => true);
        _ = module.SetupVoid("unsubscribe", _ => true).SetVoidResult();

        IRenderedComponent<FcLayoutBreakpointWatcher> cut = Render<FcLayoutBreakpointWatcher>();
        cut.WaitForAssertion(() => JSInterop.Invocations["import"].ShouldNotBeEmpty());

        await cut.Instance.DisposeAsync();

        // F8 — scope the invocation check to THIS module. LayoutComponentTestBase mocks
        // fc-beforeunload.js and fc-prefers-color-scheme.js too (both with their own `unsubscribe`
        // identifier), so a global scan was a false-positive surface. The module's own Invocations
        // list is the authoritative source for per-module call history in bUnit.
        module.Invocations
            .Any(inv => inv.Identifier.Equals("unsubscribe", StringComparison.Ordinal))
            .ShouldBeTrue("DisposeAsync MUST invoke unsubscribe on fc-layout-breakpoints.js (D5 lifecycle)");
    }
}
