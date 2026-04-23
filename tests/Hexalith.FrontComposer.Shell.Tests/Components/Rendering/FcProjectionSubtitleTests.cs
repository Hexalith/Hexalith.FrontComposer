using System.ComponentModel.DataAnnotations;
using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 4-1 T6.6 — <see cref="FcProjectionSubtitle"/> tests covering nullable-DI
/// fallback, per-type <c>CountChanged</c> filter (D21), disposal discipline (D9),
/// role-specific copy (AC6), and loading-state empty fragment (SCAMPER-M round 3).
/// </summary>
public sealed class FcProjectionSubtitleTests : LayoutComponentTestBase {
    private sealed class OrderProjection { }
    private sealed class ShipmentProjection { }

    [Display(Name = "Order", GroupName = "Orders")]
    private sealed class LabeledOrderProjection { }

    public FcProjectionSubtitleTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        // Store init is deferred to first Render per LayoutComponentTestBase design; this lets
        // individual tests Services.Replace before the container locks.
    }

    [Fact]
    public void FallsBackToCascadingCount_WhenServiceAbsent() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.FallbackCount, 42));

        cut.Markup.ShouldContain("42 orders");
    }

    [Fact]
    public void RendersActionQueueCopy_WithFallbackCount() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.Role, ProjectionRole.ActionQueue)
            .Add(p => p.FallbackCount, 3));

        cut.Markup.ShouldContain("3 orders awaiting your action");
    }

    [Fact]
    public void RendersTimelineCopy() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.Role, ProjectionRole.Timeline)
            .Add(p => p.FallbackCount, 5));

        cut.Markup.ShouldContain("5 events");
    }

    [Fact]
    public void RendersDetailRecordCopy_WithEntityName() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.Role, ProjectionRole.DetailRecord)
            .Add(p => p.FallbackCount, 1));

        cut.Markup.ShouldContain("Order overview");
    }

    [Fact]
    public void RendersStatusOverviewCopy_WithDistinctStatusCount() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.Role, ProjectionRole.StatusOverview)
            .Add(p => p.FallbackCount, 7)
            .Add(p => p.DistinctStatusCount, 3));

        cut.Markup.ShouldContain("7 total across 3 statuses");
    }

    [Fact]
    public void StatusOverviewFallsBackToDefaultCopy_WhenDistinctStatusCountMissing() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.Role, ProjectionRole.StatusOverview)
            .Add(p => p.FallbackCount, 7));

        cut.Markup.ShouldContain("7 orders");
        cut.Markup.ShouldNotContain("statuses");
    }

    [Fact]
    public void UsesProjectionDisplayAttribute_WhenGeneratorLabelsAreAbsent() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(LabeledOrderProjection))
            .Add(p => p.FallbackCount, 2));

        cut.Markup.ShouldContain("2 Orders");
    }

    [Fact]
    public void RendersEmptyFragmentDuringLoading() {
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.FallbackCount, 5)
            .Add(p => p.IsLoading, true));

        // Loading → no visible subtitle text, no "0 orders" flash-of-false-info.
        cut.Markup.ShouldNotContain("orders");
    }

    [Fact]
    public void UsesBadgeCountServiceCount_WhenRegisteredBeforeRender() {
        IBadgeCountService service = Substitute.For<IBadgeCountService>();
        service.Counts.Returns(new Dictionary<Type, int> { [typeof(OrderProjection)] = 17 });
        service.CountChanged.Returns(System.Reactive.Linq.Observable.Never<BadgeCountChangedArgs>());

        // Replace the service BEFORE any render triggers bUnit's service container lock.
        Services.Replace(ServiceDescriptor.Scoped(_ => service));

        // Force a deferred store init so Services stay mutable until now.
        IRenderedComponent<FcProjectionSubtitle> cut = Render<FcProjectionSubtitle>(parameters => parameters
            .Add(p => p.ProjectionType, typeof(OrderProjection))
            .Add(p => p.FallbackCount, 999));

        cut.Markup.ShouldContain("17 orders");
        cut.Markup.ShouldNotContain("999");
    }
}
