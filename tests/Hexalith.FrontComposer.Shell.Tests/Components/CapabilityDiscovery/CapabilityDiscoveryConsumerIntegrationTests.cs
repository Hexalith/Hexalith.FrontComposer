using System.Collections.Immutable;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Home;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.CapabilityDiscovery;

/// <summary>
/// Real consumer-seam coverage for Story 3-4 and Story 3-5 using the production
/// <see cref="Shell.Badges.BadgeCountService"/> + <see cref="State.CapabilityDiscovery.CapabilityDiscoveryEffects"/>
/// registrations instead of test-only badge stubs.
/// </summary>
public sealed class CapabilityDiscoveryConsumerIntegrationTests : LayoutComponentTestBase {
    private readonly IFrontComposerRegistry _registry;

    public CapabilityDiscoveryConsumerIntegrationTests() {
        _registry = Substitute.For<IFrontComposerRegistry>();
        _registry.GetManifests().Returns([
            new DomainManifest(
                Name: "Counter",
                BoundedContext: "Counter",
                Projections: [typeof(string).FullName!],
                Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(_registry));
        Services.Replace(ServiceDescriptor.Singleton<IActionQueueProjectionCatalog>(new StubCatalog(typeof(string))));
        Services.Replace(ServiceDescriptor.Scoped<IActionQueueCountReader>(
            _ => new StubReader((typeof(string), 7))));

        EnsureStoreInitialized();
    }

    [Fact]
    public void AppInitialized_SeedsRealBadgeService_AndPaletteConsumerShowsBadge() {
        ImmutableArray<PaletteResult> results = [
            new(
                PaletteResultCategory.Projection,
                DisplayLabel: "Counter",
                BoundedContext: "Counter",
                RouteUrl: "/counter/string",
                CommandTypeName: null,
                Score: 100,
                IsInCurrentContext: false,
                ProjectionType: typeof(string)),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        Services.GetRequiredService<IDispatcher>().Dispatch(new AppInitializedAction("c-1"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("fluent-badge");
        });
    }

    [Fact]
    public void AppInitialized_SeedsRealBadgeService_AndHomeConsumerShowsBadge() {
        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        Services.GetRequiredService<IDispatcher>().Dispatch(new AppInitializedAction("c-2"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-card-badge-Counter\"");
            cut.Markup.ShouldContain("data-testid=\"fc-home-card-projection-Counter-String\"");
        });
    }

    private sealed class StubCatalog(params Type[] actionQueueTypes) : IActionQueueProjectionCatalog {
        public IReadOnlyList<Type> ActionQueueTypes { get; } = actionQueueTypes;
    }

    private sealed class StubReader(params (Type Type, int Count)[] counts) : IActionQueueCountReader {
        private readonly ImmutableDictionary<Type, int> _counts = counts.ToImmutableDictionary(
            static entry => entry.Type,
            static entry => entry.Count);

        public ValueTask<int> GetCountAsync(Type projectionType, CancellationToken cancellationToken)
            => ValueTask.FromResult(_counts.TryGetValue(projectionType, out int count) ? count : 0);
    }
}
