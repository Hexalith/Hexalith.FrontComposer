using Bunit;

using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

public sealed class FcNewItemIndicatorTests : LayoutComponentTestBase {
    public FcNewItemIndicatorTests() => EnsureStoreInitialized();

    [Fact]
    public void RendersAccessiblePoliteIndicatorCopy() {
        IRenderedComponent<FcNewItemIndicator> cut = Render<FcNewItemIndicator>(parameters => parameters
            .Add(p => p.IndicatorId, "new-counter-1"));

        cut.Markup.ShouldContain("New item. It may not match current filters yet.");
        cut.Markup.ShouldContain("aria-live=\"polite\"");
        cut.Markup.ShouldContain("aria-describedby=\"new-counter-1-description\"");
    }

    [Fact]
    public void State_AutoDismissesAfterConfiguredDurationUsingTimeProvider() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        sut.Snapshot("view-1").Count.ShouldBe(1);

        time.Advance(TimeSpan.FromSeconds(10));

        sut.Snapshot("view-1").ShouldBeEmpty();
    }

    [Fact]
    public void State_DismissesOnFilterChangeAndMaterialization() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService sut = new(time);

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-1", "message-1", time.GetUtcNow()));
        sut.DismissMaterialized("view-1", "counter-1");
        sut.Snapshot("view-1").ShouldBeEmpty();

        sut.Add(new NewItemIndicatorEntry("view-1", "counter-2", "message-2", time.GetUtcNow()));
        sut.DismissForFilterChange("view-1");
        sut.Snapshot("view-1").ShouldBeEmpty();
    }
}
