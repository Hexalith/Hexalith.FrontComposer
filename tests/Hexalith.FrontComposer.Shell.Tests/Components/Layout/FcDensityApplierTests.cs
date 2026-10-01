// ATDD RED PHASE — Story 3-3 Task 10.6 (D9, D10; AC6; ADR-041)
// Fails at compile until Task 4.2 (FcDensityApplier component) lands.

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Density;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-3 Task 10.6 — <see cref="FcDensityApplier"/> JS-interop lifecycle tests.
/// D10 (headless component, IStateSelection projection of <c>EffectiveDensity</c>,
/// fire-and-forget <c>setDensity</c>); ADR-041 (single-source-of-truth for the body attribute).
/// </summary>
public sealed class FcDensityApplierTests : LayoutComponentTestBase {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-density.js";

    public FcDensityApplierTests() => EnsureStoreInitialized();

    [Fact]
    public void InvokesSetDensityOnInitialRender() {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupVoid("setDensity", _ => true).SetVoidResult();

        IRenderedComponent<FcDensityApplier> cut = Render<FcDensityApplier>();

        cut.WaitForAssertion(() =>
            JSInterop.Invocations["import"].ShouldNotBeEmpty(
                "Module must be imported on first render (D10)."));
        // First-render value should be the feature default (Comfortable).
        cut.WaitForAssertion(() =>
            module.Invocations.Where(i => i.Identifier == "setDensity")
                .ShouldNotBeEmpty("setDensity must be invoked at least once on initial render."));
    }

    [Theory]
    [InlineData(DensityLevel.Comfortable, DensityLevel.Compact)]
    [InlineData(DensityLevel.Compact, DensityLevel.Comfortable)]
    public async Task InvokesSetDensityOnStateChange(DensityLevel initial, DensityLevel pending) {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupVoid("setDensity", _ => true).SetVoidResult();

        IRenderedComponent<FcDensityApplier> cut = Render<FcDensityApplier>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new UserPreferenceChangedAction("initial", initial, initial));
        cut.WaitForAssertion(() => module.Invocations.Last(i => i.Identifier == "setDensity")
            .Arguments[0].ShouldBe(initial.ToString()));

        // Hold the next write after the browser receives it, then return to the last completed
        // density. Deduplication must account for the pending write before accepting that return.
        JSRuntimeInvocationHandler held = module.SetupVoid("setDensity", pending.ToString());
        dispatcher.Dispatch(new UserPreferenceChangedAction("pending", pending, pending));
        cut.WaitForAssertion(() => held.Invocations.ShouldHaveSingleItem());
        TaskCompletionSource returned = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = module.SetupVoid("setDensity", invocation => {
            if (!Equals(invocation.Arguments[0], initial.ToString())) {
                return false;
            }
            returned.TrySetResult();
            return true;
        }).SetVoidResult();
        dispatcher.Dispatch(new UserPreferenceChangedAction("return", initial, initial));
        returned.Task.IsCompleted.ShouldBeFalse("The newer write must wait for the held write.");
        held.SetVoidResult();

        // This headless component does not render after writes. Await the actual interop call
        // rather than a render-triggered assertion; retain bUnit's default one-second budget.
        await returned.Task.WaitAsync(TimeSpan.FromSeconds(1), Xunit.TestContext.Current.CancellationToken);
        module.Invocations.Last(i => i.Identifier == "setDensity")
            .Arguments[0].ShouldBe(initial.ToString(),
                "A pending older density write must not overwrite the latest selected density.");
    }

    [Fact]
    public async Task DisposesCleanly() {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupVoid("setDensity", _ => true).SetVoidResult();

        IRenderedComponent<FcDensityApplier> cut = Render<FcDensityApplier>();

        // No throws — IAsyncDisposable contract honoured (D10 + Story 3-1 FcSystemThemeWatcher pattern).
        await Should.NotThrowAsync(async () => await cut.Instance.DisposeAsync().ConfigureAwait(false));
    }
}
