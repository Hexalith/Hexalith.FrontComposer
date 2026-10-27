using Bunit;
using Hexalith.FrontComposer.Contracts.Freshness;
using Hexalith.FrontComposer.Shell.Components.Feedback;
using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Feedback;

public sealed class FcFeedbackTests : BunitContext
{
    [Theory]
    [InlineData(FcFreshnessStatus.Current, "Current domain label")]
    [InlineData(FcFreshnessStatus.Stale, "Stale domain label")]
    [InlineData(FcFreshnessStatus.Degraded, "Last known domain label")]
    public void NormalizedFreshnessPreservesCallerCopyAndPoliteSemantics(FcFreshnessStatus status, string expected)
    {
        var cut = Render<FcFreshnessIndicator>(p => p
            .Add(c => c.Freshness, new FcRecordFreshness(status))
            .Add(c => c.CurrentLabel, "Current domain label")
            .Add(c => c.StaleLabel, "Stale domain label")
            .Add(c => c.DegradedLabel, "Last known domain label"));
        cut.Find("[role='status'][aria-live='polite']").TextContent.ShouldBe(expected);
    }

    [Theory]
    [InlineData(FcLiveRegionPoliteness.Polite, "status", "polite")]
    [InlineData(FcLiveRegionPoliteness.Assertive, "alert", "assertive")]
    public void LiveRegionUsesCallerUrgencyWithoutFocusableMarkup(FcLiveRegionPoliteness politeness, string role, string live)
    {
        var cut = Render<FcLiveRegion>(p => p.Add(c => c.Politeness, politeness).Add(c => c.Message, "<unsafe>"));
        var region = cut.Find("div");
        region.GetAttribute("role").ShouldBe(role);
        region.GetAttribute("aria-live").ShouldBe(live);
        region.TextContent.ShouldBe("<unsafe>");
        cut.FindAll("[tabindex],input,button,unsafe").ShouldBeEmpty();
    }

    [Fact]
    public void NoneRendersNothing()
        => Render<FcLiveRegion>(p => p.Add(c => c.Politeness, FcLiveRegionPoliteness.None)).Markup.ShouldBeEmpty();
}
