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
public sealed class FcDensityApplierTests : LayoutComponentTestBase
{
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-density.js";

    public FcDensityApplierTests()
    {
        EnsureStoreInitialized();
    }

    [Fact]
    public void InvokesSetDensityOnInitialRender()
    {
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

    [Fact]
    public async Task InvokesSetDensityOnStateChange()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupVoid("setDensity", _ => true).SetVoidResult();

        IRenderedComponent<FcDensityApplier> cut = Render<FcDensityApplier>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        // Dispatch UserPreferenceChangedAction with newEffective = Compact (resolver pre-computed by producer).
        dispatcher.Dispatch(new UserPreferenceChangedAction("c1", DensityLevel.Compact, DensityLevel.Compact));

        await Task.Yield();
        cut.WaitForAssertion(() =>
        {
            int setDensityCalls = module.Invocations.Count(i => i.Identifier == "setDensity");
            setDensityCalls.ShouldBeGreaterThan(1, "setDensity must be invoked again on state change.");
        });
    }

    [Fact]
    public async Task DisposesCleanly()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        _ = module.SetupVoid("setDensity", _ => true).SetVoidResult();

        IRenderedComponent<FcDensityApplier> cut = Render<FcDensityApplier>();

        // No throws — IAsyncDisposable contract honoured (D10 + Story 3-1 FcSystemThemeWatcher pattern).
        await Should.NotThrowAsync(async () => await cut.Instance.DisposeAsync().ConfigureAwait(false));
    }
}
