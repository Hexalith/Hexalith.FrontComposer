using System.Collections.Immutable;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-5 Task 7.7 — capability-discovery integration on the sidebar nav.
/// Covers the click-dispatch contract (D13 ordering — synchronous reducer update).
/// </summary>
public sealed class FrontComposerNavigationCapabilityBadgeTests : LayoutComponentTestBase {
    private readonly IFrontComposerRegistry _registry;
    private readonly IUlidFactory _ulidFactory;

    public FrontComposerNavigationCapabilityBadgeTests() {
        _registry = Substitute.For<IFrontComposerRegistry>();
        _registry.GetManifests().Returns([
            new DomainManifest(
                Name: "Counter",
                BoundedContext: "Counter",
                Projections: ["Counter.Domain.Projections.CounterView"],
                Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(_registry));

        _ulidFactory = Substitute.For<IUlidFactory>();
        _ulidFactory.NewUlid().Returns("01J0NAV00000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(_ulidFactory));

        EnsureStoreInitialized();
    }

    [Fact]
    public void NavItemClick_DispatchesCapabilityVisitedAction_AndReducerWins() {
        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();
        IState<FrontComposerCapabilityDiscoveryState> state =
            Services.GetRequiredService<IState<FrontComposerCapabilityDiscoveryState>>();
        cut.Instance.HandleNavItemClickedForTest(
            "Counter",
            "proj:Counter:Counter.Domain.Projections.CounterView");

        // D13: reducer ran synchronously before any navigation; seen-set already contains the id.
        state.Value.SeenCapabilities.ShouldContain("bc:Counter");
        state.Value.SeenCapabilities.ShouldContain("proj:Counter:Counter.Domain.Projections.CounterView");
    }

    [Fact]
    public void RendersNavItem_WhenCountsEmpty_FallsBackToManifestProjections() {
        // counts.IsEmpty → VisibleProjections returns RenderableProjections (no Story-3-5 hide).
        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("/counter/counter-view");
        });
    }

    [Fact]
    public void VisibleProjections_WhenResolvedTypeMissingFromCounts_RemainsVisible() {
        DomainManifest manifest = new(
            Name: "Counter",
            BoundedContext: "Counter",
            Projections: [typeof(string).FullName!, typeof(int).FullName!],
            Commands: []);
        ImmutableDictionary<Type, int> counts = ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 0);

        List<string> visible = FrontComposerNavigation.VisibleProjections(manifest, counts);

        visible.ShouldNotContain(typeof(string).FullName!);
        visible.ShouldContain(typeof(int).FullName!);
    }
}
