using System.Reflection;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// FC-DTL aggregate detail page contract tests: the wrapper composes FcPageLayout + an optional back link
/// and routes the FC-DTL surface state so the ready body renders only for Ready/Stale/Degraded (with a
/// caller banner for stale/degraded) and every non-ready state renders its own content. It owns no domain
/// copy and stays generic over TItem.
/// </summary>
public sealed class FcAggregateDetailPageTests : LayoutComponentTestBase {
    [Fact]
    public void FcAggregateDetailPage_ComposesConstrainedLayoutRootAndBackLink() {
        IRenderedComponent<FcAggregateDetailPage<string>> cut = RenderDetail(parameters => parameters
            .Add(page => page.State, FcAggregateDetailState.Ready)
            .Add(page => page.RootTestId, "detail-root")
            .Add(page => page.BackHref, "/tenants?search=alpha")
            .Add(page => page.BackLinkLabel, "Back to tenants")
            .Add(page => page.BackLinkTestId, "detail-back")
            .Add(page => page.BackLinkClass, "tenant-detail__back")
            .Add(page => page.ReadyContent, Markup("ready", "ready body")));

        cut.FindComponent<FcPageLayout>().Instance.Mode.ShouldBe(FcPageLayoutMode.Constrained);
        cut.Find("[data-testid='detail-root']").ShouldNotBeNull();

        IElement back = cut.Find("[data-testid='detail-back']");
        back.TagName.ShouldBe("A");
        back.GetAttribute("href").ShouldBe("/tenants?search=alpha");
        back.TextContent.ShouldContain("Back to tenants");
        back.ClassList.ShouldContain("fc-aggregate-detail__back");
        back.ClassList.ShouldContain("tenant-detail__back");
    }

    [Fact]
    public void FcAggregateDetailPage_HidesBackLinkWhenDisabled() {
        IRenderedComponent<FcAggregateDetailPage<string>> cut = RenderDetail(parameters => parameters
            .Add(page => page.State, FcAggregateDetailState.Ready)
            .Add(page => page.ShowBackLink, false)
            .Add(page => page.ReadyContent, Markup("ready", "ready body")));

        cut.FindAll("[data-testid='fc-aggregate-detail-back']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(FcAggregateDetailState.Loading, "loading")]
    [InlineData(FcAggregateDetailState.Unauthorized, "unauthorized")]
    [InlineData(FcAggregateDetailState.NotFound, "notfound")]
    [InlineData(FcAggregateDetailState.Unavailable, "unavailable")]
    public void FcAggregateDetailPage_RendersOnlyTheMatchingNonReadyState(FcAggregateDetailState state, string expectedSlot) {
        IRenderedComponent<FcAggregateDetailPage<string>> cut = RenderDetail(parameters => parameters
            .Add(page => page.State, state)
            .Add(page => page.LoadingContent, Markup("loading", "loading"))
            .Add(page => page.UnauthorizedContent, Markup("unauthorized", "unauthorized"))
            .Add(page => page.NotFoundContent, Markup("notfound", "not found"))
            .Add(page => page.UnavailableContent, Markup("unavailable", "unavailable"))
            .Add(page => page.ReadyContent, Markup("ready", "ready body")));

        cut.Find($"[data-testid='{expectedSlot}']").ShouldNotBeNull();
        cut.FindAll("[data-testid='ready']").ShouldBeEmpty("a non-ready state must never render the ready body");
        foreach (string slot in new[] { "loading", "unauthorized", "notfound", "unavailable" }) {
            if (slot != expectedSlot) {
                cut.FindAll($"[data-testid='{slot}']").ShouldBeEmpty();
            }
        }
    }

    [Fact]
    public void FcAggregateDetailPage_Ready_RendersBodyWithoutBanners() {
        IRenderedComponent<FcAggregateDetailPage<string>> cut = RenderDetail(parameters => parameters
            .Add(page => page.State, FcAggregateDetailState.Ready)
            .Add(page => page.StaleBanner, Markup("stale", "stale"))
            .Add(page => page.DegradedBanner, Markup("degraded", "degraded"))
            .Add(page => page.ReadyContent, Markup("ready", "ready body")));

        cut.Find("[data-testid='ready']").ShouldNotBeNull();
        cut.FindAll("[data-testid='stale']").ShouldBeEmpty();
        cut.FindAll("[data-testid='degraded']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(FcAggregateDetailState.Stale, "stale")]
    [InlineData(FcAggregateDetailState.Degraded, "degraded")]
    public void FcAggregateDetailPage_StaleAndDegraded_RenderBannerAboveReadyBody(FcAggregateDetailState state, string bannerSlot) {
        IRenderedComponent<FcAggregateDetailPage<string>> cut = RenderDetail(parameters => parameters
            .Add(page => page.State, state)
            .Add(page => page.StaleBanner, Markup("stale", "stale banner"))
            .Add(page => page.DegradedBanner, Markup("degraded", "degraded banner"))
            .Add(page => page.ReadyContent, Markup("ready", "ready body")));

        cut.Find($"[data-testid='{bannerSlot}']").ShouldNotBeNull();
        cut.Find("[data-testid='ready']").ShouldNotBeNull();

        // The banner renders before the ready body.
        string markup = cut.Markup;
        markup.IndexOf(bannerSlot, StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("ready body", StringComparison.Ordinal));
    }

    [Fact]
    public void FcAggregateDetailPage_ReadyTemplate_ReceivesTypedItem() {
        IRenderedComponent<FcAggregateDetailPage<string>> cut = RenderDetail(parameters => parameters
            .Add(page => page.State, FcAggregateDetailState.Ready)
            .Add(page => page.Item, "tenant.alpha")
            .Add(page => page.ReadyTemplate, (RenderFragment<string>)(item =>
                builder => builder.AddMarkupContent(0, $"<span data-testid=\"ready-template\">{item}</span>"))));

        cut.Find("[data-testid='ready-template']").TextContent.ShouldBe("tenant.alpha");
    }

    [Fact]
    public void FcAggregateDetailPage_FailsClosed_WhenSpecificStateSlotMissing() {
        IRenderedComponent<FcAggregateDetailPage<string>> cut = RenderDetail(parameters => parameters
            .Add(page => page.State, FcAggregateDetailState.Unauthorized)
            .Add(page => page.UnavailableContent, Markup("unavailable", "unavailable"))
            .Add(page => page.ReadyContent, Markup("ready", "ready body")));

        // No UnauthorizedContent supplied → fail closed to the unavailable content, never the ready body.
        cut.Find("[data-testid='unavailable']").ShouldNotBeNull();
        cut.FindAll("[data-testid='ready']").ShouldBeEmpty();
    }

    [Fact]
    public void FcAggregateDetailPage_TypeContract_DoesNotInjectDomainResources() {
        PropertyInfo[] injectedProperties = typeof(FcAggregateDetailPage<string>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static property => property.GetCustomAttribute<InjectAttribute>() is not null)
            .ToArray();

        injectedProperties.ShouldBeEmpty("FcAggregateDetailPage must stay domain-agnostic; consumers pass strings and fragments.");
    }

    private IRenderedComponent<FcAggregateDetailPage<string>> RenderDetail(
        Action<ComponentParameterCollectionBuilder<FcAggregateDetailPage<string>>> parameters)
        => Render(parameters);

    private static RenderFragment Markup(string testId, string text)
        => builder => builder.AddMarkupContent(0, $"<span data-testid=\"{testId}\">{text}</span>");
}
