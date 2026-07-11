
// Story 3-4 Task 10.8 (D14 / D16 / ADR-044 — AC3 / AC5 / AC6 / AC7).
using System.Collections.Immutable;

using Bunit;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class FcPaletteResultListTests : LayoutComponentTestBase {
    public FcPaletteResultListTests() => System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en");

    [Fact]
    public void RendersAllRegisteredCategoriesWithHeadings() {
        EnsureStoreInitialized();
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Projection, "Counter", "Counter", "/counter/counter", null, 100, false, typeof(CounterProjectionStub)),
            new(PaletteResultCategory.Command, "Increment", "Counter", null, "Counter.IncrementCommand", 90, false),
            new(PaletteResultCategory.Recent, "/orders", "", "/orders", null, 0, false),
            new(PaletteResultCategory.Shortcut, "Ctrl+K", "", null, null, 0, false, null, "PaletteShortcutDescription"),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldContain("Projections");
        cut.Markup.ShouldContain("Commands");
        cut.Markup.ShouldContain("Recent");
        cut.Markup.ShouldContain("Keyboard shortcuts");
    }

    [Fact]
    public void RendersWithoutBadges_WhenBadgeServiceIsNull() {
        EnsureStoreInitialized();
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Projection, "Counter", "Counter", "/c", null, 100, false, typeof(CounterProjectionStub)),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldNotContain("fluent-badge");
    }

    [Fact]
    public void RendersBadges_WhenBadgeServiceIsRegistered_AndProjectionMatches() {
        Services.AddSingleton<IBadgeCountService>(new StubBadgeService((typeof(CounterProjectionStub), 7)));
        EnsureStoreInitialized();

        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Projection, "Counter", "Counter", "/c", null, 100, false, typeof(CounterProjectionStub)),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldContain("fluent-badge");
        cut.Markup.ShouldContain("7");
    }

    [Fact]
    public void NoBadgePlaceholder_WhenProjectionTypeNotInCounts() {
        Services.AddSingleton<IBadgeCountService>(new StubBadgeService());
        EnsureStoreInitialized();

        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Projection, "Counter", "Counter", "/c", null, 100, false, typeof(CounterProjectionStub)),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldNotContain("fluent-badge");
    }

    [Fact]
    public void ShortcutRowsCarryAriaDisabledTrue() {
        EnsureStoreInitialized();
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Shortcut, "Ctrl+K", "", null, null, 0, false, null, "PaletteShortcutDescription"),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldContain("aria-disabled=\"true\"");
    }

    [Fact]
    public void SyntheticKeyboardShortcutsEntry_RendersDescriptionHint() {
        EnsureStoreInitialized();
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Command, "Keyboard Shortcuts", "", null, CommandPaletteEffects.KeyboardShortcutsSentinel, 1000, false, null, "KeyboardShortcutsCommandDescription"),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldContain("View all keyboard shortcuts");
    }

    [Fact]
    public void InformationalShortcutRowsDoNotInvokeSelectionCallback() {
        EnsureStoreInitialized();
        int invocations = 0;
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Shortcut, "Ctrl+K", "", null, null, 0, false, null, "PaletteShortcutDescription"),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => invocations++));

        cut.Find("[data-testid='fc-palette-option']").Click();

        invocations.ShouldBe(0);
    }

    [Fact]
    public void RoutableCommandRowInvokesSelectionCallback() {
        EnsureStoreInitialized();
        int selectedIndex = -1;
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Command, "Configure Counter", "Counter", null, "Counter.ConfigureCounterCommand", 100, false),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, index => selectedIndex = index));

        cut.Find("[data-testid='fc-palette-option']").Click();

        selectedIndex.ShouldBe(0);
        cut.Markup.ShouldNotContain("@onclick");
    }

    [Fact]
    public void ShortcutRowWithRouteUrl_DoesNotCarryAriaDisabled() {
        // Pass-5 P20 (C1-D2 resolution) — shortcut-with-route rows are activatable per D11;
        // only informational (null RouteUrl) shortcut rows get aria-disabled="true".
        EnsureStoreInitialized();
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Shortcut, "g h", "", "/", null, 0, false, null, "HomeShortcutDescription"),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldNotContain("aria-disabled=\"true\"");
    }

    [Fact]
    public void EmptyResults_RendersNoMatchesText() {
        EnsureStoreInitialized();
        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, ImmutableArray<PaletteResult>.Empty)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldContain("No matches found");
    }

    [Fact]
    public void ResultsContainer_RendersRoleListbox_WithRoleOptionRows() {
        // Story 2.7 Task 1 (AC1) — default-lane pin for the listbox/option ROLE set. Pre-existing pins
        // asserted headings / badges / aria-disabled but never role="listbox" on the container or
        // role="option" on the rows (verified gap).
        EnsureStoreInitialized();
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Projection, "Counter", "Counter", "/counter/counter", null, 100, false, typeof(CounterProjectionStub)),
            new(PaletteResultCategory.Command, "Increment", "Counter", null, "Counter.IncrementCommand", 90, false),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 0)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldContain("role=\"listbox\"");
        cut.Markup.ShouldContain("role=\"option\"");
    }

    [Fact]
    public void AriaSelected_AndAriaActiveDescendant_TrackSelectedIndex() {
        // Story 2.7 Task 1 (AC1) — default-lane pin proving aria-activedescendant + aria-selected
        // track SelectedIndex: the selected <li role="option"> carries the matching id +
        // aria-selected="true", the listbox's aria-activedescendant points at it, and the
        // non-selected row carries NO aria-selected.
        EnsureStoreInitialized();
        ImmutableArray<PaletteResult> results = [
            new(PaletteResultCategory.Projection, "Counter", "Counter", "/counter/counter", null, 100, false, typeof(CounterProjectionStub)),
            new(PaletteResultCategory.Projection, "Orders", "Orders", "/orders/orders", null, 90, false, typeof(CounterProjectionStub)),
        ];

        IRenderedComponent<FcPaletteResultList> cut = Render<FcPaletteResultList>(p => p
            .Add(c => c.Id, "fc-palette-results")
            .Add(c => c.Results, results)
            .Add(c => c.SelectedIndex, 1)
            .Add(c => c.OnSelectionChanged, _ => { }));

        cut.Markup.ShouldContain("aria-activedescendant=\"fc-palette-result-1\"");

        AngleSharp.Dom.IElement selected = cut.Find("#fc-palette-result-1");
        selected.GetAttribute("role").ShouldBe("option");
        selected.GetAttribute("aria-selected").ShouldBe("true");

        AngleSharp.Dom.IElement other = cut.Find("#fc-palette-result-0");
        other.GetAttribute("aria-selected").ShouldBeNull();
    }

    private sealed class StubBadgeService : IBadgeCountService {
        public StubBadgeService(params (Type Type, int Count)[] entries) {
            Counts = entries.ToDictionary(e => e.Type, e => e.Count, EqualityComparer<Type>.Default);
            TotalActionableItems = entries.Sum(e => e.Count);
            CountChanged = new NoOpObservable();
        }

        public IReadOnlyDictionary<Type, int> Counts { get; }

        public IObservable<BadgeCountChangedArgs> CountChanged { get; }

        public int TotalActionableItems { get; }

        private sealed class NoOpObservable : IObservable<BadgeCountChangedArgs> {
            public IDisposable Subscribe(IObserver<BadgeCountChangedArgs> observer) => new NoOpDisposable();

            private sealed class NoOpDisposable : IDisposable {
                public void Dispose() { }
            }
        }
    }

    private sealed class CounterProjectionStub { }
}
