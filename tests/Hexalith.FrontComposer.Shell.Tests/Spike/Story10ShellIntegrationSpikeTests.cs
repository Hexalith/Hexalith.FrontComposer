using System.Reflection;

using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Spike;

/// <summary>
/// Story 1.0 — Shell-integration spike regression suite.
///
/// The spike (<c>_bmad-output/spike-notes/1-0-shell-integration-spike-2026-06-03.md</c>) was a
/// throwaway investigation that *exercised and confirmed* four already-shipped Shell behaviours so
/// Story 1.1 (bootstrap) could start from facts. The throwaway host was discarded; these tests pin
/// the four confirmed API answers against the live <c>src/</c> code so they cannot silently regress
/// before 1.1 consumes them. Each test maps to one 🔴 AR5 question from the spike note.
/// </summary>
public sealed class Story10ShellIntegrationSpikeTests
{
    // ── Q1: AddHexalithFrontComposer* registration path boots an empty shell (AC#1/#2) ─────────────
    // The spike confirmed the canonical 3-call ordering
    //   AddHexalithFrontComposerQuickstart() → AddHexalithDomain<CounterDomain>() → stub AddHexalithEventStore()
    // builds a working container with scope validation on (ADR-030 scoped-lifetime discipline holds).

    [Fact]
    public void Bootstrap_QuickstartThenDomainThenStubEventStore_BuildsWithScopeValidation()
    {
        using ServiceProvider provider = BuildSpikeHost();

        // The authoritative registry resolves (singleton) — empty shell can compose against it.
        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();
        _ = registry.ShouldNotBeNull();

        // A scoped service resolves cleanly inside a scope — proves ValidateScopes found no
        // singleton-captures-scoped regression (the ADR-030 guard the spike emphasised).
        using IServiceScope scope = provider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IStorageService>().ShouldNotBeNull();
    }

    [Fact]
    public void Bootstrap_EventStoreRegisteredAfterQuickstart_PreservesAuthoritativeRegistry()
    {
        // The spike's ordering invariant: AddHexalithEventStore only TryAdds IFrontComposerRegistry,
        // so the registry Quickstart installed (and AddHexalithDomain populated) survives EventStore
        // registration — the domain manifests are NOT dropped when EventStore runs last.
        using ServiceProvider provider = BuildSpikeHost();

        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();
        registry.GetManifests()
            .ShouldContain(m => m.BoundedContext == "Counter",
                "EventStore's TryAdd must not replace the Quickstart-installed registry that already holds the Counter manifest.");
    }

    // ── Q2: Manifest discovery → GetManifests() (AC#2) ─────────────────────────────────────────────
    // The spike confirmed AddHexalithDomain<TMarker> reflects the marker assembly for generated
    // *Registration types and flows them into IFrontComposerRegistry.GetManifests() automatically —
    // verified here through the full Quickstart entry point the spike actually booted.

    [Fact]
    public void ManifestDiscovery_ThroughQuickstart_SurfacesGeneratedCounterRegistration()
    {
        using ServiceProvider provider = BuildSpikeHost();

        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();
        IReadOnlyList<DomainManifest> manifests = registry.GetManifests();

        manifests.Count.ShouldBeGreaterThanOrEqualTo(1, "AC#2: at least one DomainManifest from a generated *Registration.");

        DomainManifest counter = manifests.Single(m => m.BoundedContext == "Counter");
        counter.Projections.ShouldContain(typeof(CounterProjection).FullName!);
        counter.Commands.ShouldContain(typeof(IncrementCommand).FullName!);
    }

    // ── Q3: Projection-route reachability + companion-interface opt-in (Task 3) ─────────────────────
    // The spike's headline routing finding: the DEFAULT FrontComposerRegistry already implements the
    // route-reachability companion (IFrontComposerFullPageRouteRegistry) and the write-access
    // companion, so stock hosts need no extra wiring for full-page routes (Story 3-4 D21 / DN6).

    [Fact]
    public void DefaultRegistry_ImplementsRouteReachabilityCompanions()
    {
        using ServiceProvider provider = BuildSpikeHost();

        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();

        registry.ShouldBeAssignableTo<IFrontComposerFullPageRouteRegistry>(
            "Stock registry must satisfy the full-page route-reachability companion so adopters need no extra wiring (spike Q3).");
        registry.ShouldBeAssignableTo<IFrontComposerCommandWriteAccessRegistry>(
            "Stock registry must satisfy the write-access companion (empty-state CTA eligibility).");
    }

    [Fact]
    public void DefaultRegistry_HasFullPageRoute_TrueForRegisteredCommand_FalseForUnknown()
    {
        using ServiceProvider provider = BuildSpikeHost();

        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();

        // The default HasFullPageRoute returns true for any command present in a manifest (inert
        // permissive placeholder until Story 9-4), and false for a command that was never registered.
        registry.HasFullPageRoute(typeof(IncrementCommand).FullName!).ShouldBeTrue();
        registry.HasFullPageRoute("Counter.Domain.NeverRegisteredCommand").ShouldBeFalse();
    }

    // ── Q4: FC-TBL column/filter/expand surface (Task 4 — feeds Story 2.8) ──────────────────────────
    // The spike recorded the adopter-facing DataGrid surface as a set of public ComponentBase
    // sub-components that Story 2.8 must mark confirmed-stable (finding F3: not yet frozen in a
    // PublicAPI.Shipped.txt). This test pins that surface at compile time — renaming or hiding any
    // component breaks the build, which is the exact signal Story 2.8 needs.

    public static readonly Type[] FcTblPublicSurface =
    [
        typeof(FcColumnFilterCell),
        typeof(FcColumnPrioritizer),
        typeof(FcExpandInRowDetail),
        typeof(FcExpandedRowHiddenBanner),
        typeof(FcFilterEmptyState),
        typeof(FcFilterResetButton),
        typeof(FcFilterSummary),
        typeof(FcMaxItemsCapNotice),
        typeof(FcNewItemIndicator),
        typeof(FcProjectionGlobalSearch),
        typeof(FcSlowQueryNotice),
        typeof(FcStatusFilterChips),
    ];

    [Fact]
    public void FcTbl_DocumentedSurface_IsPublicComponentBase()
    {
        foreach (Type component in FcTblPublicSurface)
        {
            component.IsPublic.ShouldBeTrue($"{component.Name} is part of the adopter-facing FC-TBL surface and must stay public (spike Q4 / Story 2.8).");
            component.IsAssignableTo(typeof(ComponentBase)).ShouldBeTrue($"{component.Name} must be a Blazor ComponentBase.");
        }
    }

    [Fact]
    public void FcColumnPrioritizer_MaxVisibleColumns_DefaultIsTen()
    {
        // The spike pinned the wide-grid activation default (>15 columns → HFC1028/HFC1029) — the
        // visible-column ceiling defaults to 10.
        new FcColumnPrioritizer().MaxVisibleColumns.ShouldBe(10);
    }

    [Fact]
    public void FcExpandInRowDetail_ExposesDocumentedParameters()
    {
        // WCAG 4.1.2 hidden-expansion contract the spike recorded: PanelId + SuppressedAnnouncement
        // are bindable [Parameter]s on the expand-in-row component.
        ShouldHaveParameter(typeof(FcExpandInRowDetail), "PanelId");
        ShouldHaveParameter(typeof(FcExpandInRowDetail), "SuppressedAnnouncement");
    }

    // ── Shared spike-host fixture ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors the spike's throwaway host wiring in a unit-test container: the canonical 3-call
    /// ordering with the default <see cref="IStorageService"/> swapped for the in-memory
    /// implementation (the real LocalStorageService needs IJSRuntime), built with
    /// <c>ValidateScopes = true</c> to keep the ADR-030 scoped-lifetime guard active.
    /// </summary>
    private static ServiceProvider BuildSpikeHost()
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddHexalithFrontComposerQuickstart();
        _ = services.AddHexalithDomain<CounterDomain>();
        _ = services.AddHexalithEventStore(o =>
        {
            // Stub backend — absolute BaseAddress passes ValidateOnStart without a live EventStore.
            o.BaseAddress = new Uri("http://localhost:9/");
            o.RequireAccessToken = false;
        });
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void ShouldHaveParameter(Type component, string propertyName)
    {
        PropertyInfo? property = component.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        _ = property.ShouldNotBeNull($"{component.Name}.{propertyName} must exist as a public property.");
        property.GetCustomAttribute<ParameterAttribute>()
            .ShouldNotBeNull($"{component.Name}.{propertyName} must be a bindable [Parameter] (spike Q4 contract).");
    }
}
