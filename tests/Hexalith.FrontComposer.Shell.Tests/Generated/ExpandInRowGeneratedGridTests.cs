#pragma warning disable CA2007
using AngleSharp.Dom;

using Bunit;

using Counter.Domain;

using Fluxor;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2.4 AC1/AC2 — runtime pins for the generated grid expand-in-row accessibility path.
/// Component, reducer, and emitter tests already pin the isolated pieces; these tests render the
/// generated Counter grid and exercise the click/filter flow end-to-end in bUnit.
/// </summary>
public sealed class ExpandInRowGeneratedGridTests : GeneratedComponentTestBase
{
    private static readonly DateTimeOffset s_lastUpdated = new(2026, 4, 14, 0, 0, 0, TimeSpan.Zero);

    public ExpandInRowGeneratedGridTests()
        : base(typeof(CounterProjection).Assembly)
    {
    }

    [Fact]
    public async Task CounterProjectionView_ExpandTrigger_PopulatesAlwaysPresentRegion()
    {
        SetupExpandInRowModule();
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-ac1",
            [
                Counter("counter-1", 1),
                Counter("counter-2", 2),
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("role").ShouldBe("region");
            region.GetAttribute("aria-label").ShouldBe("Details for ");
            region.TextContent.ShouldNotContain("counter-1");

            IElement button = ExpandButton(cut, "counter-1");
            button.GetAttribute("aria-expanded").ShouldBe("false");
            string? controls = button.GetAttribute("aria-controls");
            controls.ShouldNotBeNull();
            controls.ShouldNotBeWhiteSpace();
            region.GetAttribute("id").ShouldBe(controls);
        });

        await cut.InvokeAsync(() => ExpandButton(cut, "counter-1").Click());

        await cut.WaitForAssertionAsync(() =>
        {
            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("role").ShouldBe("region");
            region.GetAttribute("aria-label").ShouldBe("Details for counter-1");
            region.TextContent.ShouldContain("counter-1");
            region.GetAttribute("id").ShouldBe(CollapseButton(cut, "counter-1").GetAttribute("aria-controls"));
            CollapseButton(cut, "counter-1").GetAttribute("aria-expanded").ShouldBe("true");
            ExpandButton(cut, "counter-2").GetAttribute("aria-expanded").ShouldBe("false");
        });
    }

    [Fact]
    public async Task CounterProjectionView_FilterHidesExpandedRow_RendersBannerAndSuppressedAnnouncement()
    {
        SetupExpandInRowModule();
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-ac2",
            [
                Counter("counter-1", 1),
                Counter("counter-2", 2),
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() => _ = ExpandButton(cut, "counter-1"));
        await cut.InvokeAsync(() => ExpandButton(cut, "counter-1").Click());
        await cut.WaitForAssertionAsync(() => cut.Find(".fc-expand-in-row-detail").TextContent.ShouldContain("counter-1"));

        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-filtered",
            [
                Counter("counter-2", 2),
            ])));

        await cut.WaitForAssertionAsync(() =>
        {
            IElement banner = cut.Find("[data-testid='fc-expanded-row-hidden-banner']");
            banner.GetAttribute("role").ShouldBe("status");
            banner.GetAttribute("aria-live").ShouldBe("polite");
            banner.TextContent.ShouldContain("1 expanded item hidden by current filter");
            banner.TextContent.ShouldContain("Clear filter");

            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("role").ShouldBe("region");
            region.GetAttribute("aria-label").ShouldBe("Details for counter-1");
            region.TextContent.ShouldNotContain("counter-1");

            IElement liveRegion = cut.Find(".fc-expand-in-row-suppression-live-region");
            liveRegion.GetAttribute("role").ShouldBe("status");
            liveRegion.GetAttribute("aria-live").ShouldBe("polite");
            liveRegion.GetAttribute("aria-atomic").ShouldBe("true");
            liveRegion.TextContent.ShouldContain("Your expanded item is hidden by the current filter");
        });
    }

    private void SetupExpandInRowModule()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
    }

    private static CounterProjection Counter(string id, int count)
        => new()
        {
            Id = id,
            Count = count,
            LastUpdated = s_lastUpdated,
        };

    private static IElement ExpandButton(IRenderedComponent<CounterProjectionView> cut, string id)
        => Button(cut, $"Expand details for {id}");

    private static IElement CollapseButton(IRenderedComponent<CounterProjectionView> cut, string id)
        => Button(cut, $"Collapse details for {id}");

    private static IElement Button(IRenderedComponent<CounterProjectionView> cut, string ariaLabel)
        => cut.Nodes.QuerySelectorAll("fluent-button.fc-expand-button")
            .Single(button => string.Equals(button.GetAttribute("aria-label"), ariaLabel, StringComparison.Ordinal));
}
