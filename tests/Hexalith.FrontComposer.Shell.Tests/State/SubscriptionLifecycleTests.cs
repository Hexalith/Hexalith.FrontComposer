
using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Theme;
using Hexalith.FrontComposer.Shell.Tests.Components;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State;
/// <summary>
/// bUnit tests verifying the IState&lt;T&gt; + IDisposable subscription lifecycle pattern (AC2).
/// </summary>
public class SubscriptionLifecycleTests : FrontComposerTestBase {
    [Fact]
    public async Task ThemeSubscription_ComponentRendered_ReceivesInitialState() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();

        // Act
        IRenderedComponent<TestThemeComponent> cut = Render<TestThemeComponent>();

        // Assert
        cut.Find("#theme-display").TextContent.ShouldBe("Light");
    }

    [Fact]
    public async Task ThemeSubscription_ActionDispatched_ComponentRerendersWithNewState() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IRenderedComponent<TestThemeComponent> cut = Render<TestThemeComponent>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        // Act
        dispatcher.Dispatch(new ThemeChangedAction("test-1", ThemeValue.Dark));
        await Task.Delay(100, ct);

        // Assert
        cut.Find("#theme-display").TextContent.ShouldBe("Dark");
    }

    [Fact]
    public async Task ThemeSubscription_ComponentDisposed_UnsubscribesFromStateChanged() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IRenderedComponent<TestThemeComponent> cut = Render<TestThemeComponent>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        int renderCountBeforeDispose = cut.RenderCount;

        // Act — dispose the component by disposing the bunit context's rendered components
        cut.Dispose();

        // Dispatch after dispose — should not throw ObjectDisposedException
        dispatcher.Dispatch(new ThemeChangedAction("test-2", ThemeValue.System));
        await Task.Delay(100, ct);

        // Assert — no additional render happened
        cut.RenderCount.ShouldBe(renderCountBeforeDispose);
    }

    [Fact]
    public async Task DensitySubscription_ComponentRendered_ReceivesInitialState() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();

        // Act
        IRenderedComponent<TestDensityComponent> cut = Render<TestDensityComponent>();

        // Assert
        cut.Find("#density-display").TextContent.ShouldBe("Comfortable");
    }

    [Fact]
    public async Task DensitySubscription_ActionDispatched_ComponentRerendersWithNewState() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IRenderedComponent<TestDensityComponent> cut = Render<TestDensityComponent>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        // Act
        dispatcher.Dispatch(new DensityChangedAction("test-3", DensityLevel.Compact));
        await Task.Delay(100, ct);

        // Assert
        cut.Find("#density-display").TextContent.ShouldBe("Compact");
    }

    [Fact]
    public async Task DensitySubscription_ComponentDisposed_UnsubscribesFromStateChanged() {
        // Arrange
        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        await InitializeStoreAsync();
        IRenderedComponent<TestDensityComponent> cut = Render<TestDensityComponent>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        int renderCountBeforeDispose = cut.RenderCount;

        // Act
        cut.Dispose();
        dispatcher.Dispatch(new DensityChangedAction("test-4", DensityLevel.Roomy));
        await Task.Delay(100, ct);

        // Assert
        cut.RenderCount.ShouldBe(renderCountBeforeDispose);
    }
}
