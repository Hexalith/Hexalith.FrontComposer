using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 2.6 AC1(b) integration pin — proves the new-item-indicator <em>producer-state → consumer-component</em>
/// contract composes end-to-end: an entry pushed to <see cref="INewItemIndicatorStateService"/> for a lane
/// surfaces a rendered <see cref="FcNewItemIndicator"/> (correct ARIA + localized copy) for that lane, and the
/// indicator disappears on every dismissal trigger (10s TTL, materialization, filter-change).
/// </summary>
/// <remarks>
/// This pins the half of AC1(b) that is in-scope and testable for Story 2.6 (the projection read path). The
/// <em>production producer</em> — a caller that constructs a <see cref="NewItemIndicatorEntry"/> with a real
/// <c>EntityKey</c>/<c>MessageId</c> when a confirmed-created row arrives outside current filters — lives in the
/// command-lifecycle / pending-command resolution path (Story 5-5 DN1, Epic 3/5), which Story 2.6 explicitly
/// does NOT reopen. The live nudge seam (<c>IProjectionHubConnection.OnProjectionChanged</c>) carries only
/// <c>(projectionType, tenantId)</c> — no per-row identity — so the indicator cannot be produced from the nudge
/// path without fabricating identities. The <see cref="LaneHost"/> below stands in for the eventual generated /
/// shell-level consumer (deferred), reading <see cref="INewItemIndicatorStateService.Snapshot(string)"/> exactly
/// as that consumer will.
/// </remarks>
public sealed class FcNewItemIndicatorLaneIntegrationTests : LayoutComponentTestBase {
    private const string ViewKey = "counter:test-tenant";
    private const string IndicatorTestId = "[data-testid=\"fc-new-item-indicator\"]";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));

    public FcNewItemIndicatorLaneIntegrationTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_time));
        EnsureStoreInitialized();
    }

    [Fact]
    public void AddingLaneEntry_RendersAccessiblePoliteIndicatorForThatLane() {
        INewItemIndicatorStateService state = Services.GetRequiredService<INewItemIndicatorStateService>();
        state.Add(new NewItemIndicatorEntry(ViewKey, "counter-1", "message-1", _time.GetUtcNow()));

        IRenderedComponent<LaneHost> cut = Render<LaneHost>(parameters => parameters.Add(p => p.ViewKey, ViewKey));

        AngleSharp.Dom.IElement indicator = cut.Find(IndicatorTestId);
        indicator.GetAttribute("role").ShouldBe("status");
        indicator.GetAttribute("aria-live").ShouldBe("polite");
        indicator.GetAttribute("aria-label").ShouldBe("New item added outside current filters");
        indicator.TextContent.Trim().ShouldBe("New item. It may not match current filters yet.");
    }

    [Fact]
    public void OtherLaneEntry_DoesNotRenderIndicatorForThisLane() {
        INewItemIndicatorStateService state = Services.GetRequiredService<INewItemIndicatorStateService>();
        state.Add(new NewItemIndicatorEntry("counter:other-tenant", "counter-9", "message-9", _time.GetUtcNow()));

        IRenderedComponent<LaneHost> cut = Render<LaneHost>(parameters => parameters.Add(p => p.ViewKey, ViewKey));

        cut.FindAll(IndicatorTestId).ShouldBeEmpty();
    }

    [Fact]
    public void Indicator_AutoDismissesAfterTtl_UsingTimeProvider() {
        INewItemIndicatorStateService state = Services.GetRequiredService<INewItemIndicatorStateService>();
        state.Add(new NewItemIndicatorEntry(ViewKey, "counter-1", "message-1", _time.GetUtcNow()));

        IRenderedComponent<LaneHost> cut = Render<LaneHost>(parameters => parameters.Add(p => p.ViewKey, ViewKey));
        cut.FindAll(IndicatorTestId).Count.ShouldBe(1);

        _time.Advance(TimeSpan.FromSeconds(10));
        cut.Render();

        cut.FindAll(IndicatorTestId).ShouldBeEmpty();
    }

    [Fact]
    public void Indicator_DismissesOnMaterialization() {
        INewItemIndicatorStateService state = Services.GetRequiredService<INewItemIndicatorStateService>();
        state.Add(new NewItemIndicatorEntry(ViewKey, "counter-1", "message-1", _time.GetUtcNow()));

        IRenderedComponent<LaneHost> cut = Render<LaneHost>(parameters => parameters.Add(p => p.ViewKey, ViewKey));
        cut.FindAll(IndicatorTestId).Count.ShouldBe(1);

        state.DismissMaterialized(ViewKey, "counter-1");
        cut.Render();

        cut.FindAll(IndicatorTestId).ShouldBeEmpty();
    }

    [Fact]
    public void Indicator_DismissesOnFilterChange() {
        INewItemIndicatorStateService state = Services.GetRequiredService<INewItemIndicatorStateService>();
        state.Add(new NewItemIndicatorEntry(ViewKey, "counter-1", "message-1", _time.GetUtcNow()));

        IRenderedComponent<LaneHost> cut = Render<LaneHost>(parameters => parameters.Add(p => p.ViewKey, ViewKey));
        cut.FindAll(IndicatorTestId).Count.ShouldBe(1);

        state.DismissForFilterChange(ViewKey);
        cut.Render();

        cut.FindAll(IndicatorTestId).ShouldBeEmpty();
    }

    /// <summary>
    /// Minimal stand-in consumer that renders one <see cref="FcNewItemIndicator"/> per snapshot entry for its
    /// lane, exactly as the deferred generated / shell-level grid consumer will. Test-only; not a <c>src/</c>
    /// component.
    /// </summary>
    private sealed class LaneHost : ComponentBase {
        [Inject]
        private INewItemIndicatorStateService State { get; set; } = default!;

        [Parameter]
        public string ViewKey { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            int seq = 0;
            foreach (NewItemIndicatorEntry entry in State.Snapshot(ViewKey)) {
                builder.OpenComponent<FcNewItemIndicator>(seq++);
                builder.SetKey(entry.EntityKey);
                builder.CloseComponent();
            }
        }
    }
}
