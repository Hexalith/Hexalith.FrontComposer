using System.Globalization;
using System.Text.RegularExpressions;

using Bunit;

using Counter.Domain;
using Counter.Web.Components.Replacements;
using Counter.Web.Components.Slots;
using Counter.Web.Components.Pages;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;
using Hexalith.FrontComposer.Shell.Services.ProjectionTemplates;
using Hexalith.FrontComposer.Shell.Services.ProjectionViewOverrides;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class CounterStoryVerificationTests : GeneratedComponentTestBase
{
    // GC-P5 — pin TimeProvider to a deterministic instant well outside the [RelativeTime]
    // 7-day window relative to LastUpdated = 2026-04-14 so the formatter falls back to absolute
    // date format ("04/14/2026") regardless of the wall clock when the test runs.
    private static readonly DateTimeOffset s_fixedNow = new(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset s_lastUpdated = new(2026, 4, 14, 0, 0, 0, TimeSpan.Zero);

    public CounterStoryVerificationTests()
        : base(typeof(CounterProjection).Assembly, typeof(StatusProjection).Assembly)
    {
    }

    [Fact]
    public async Task CounterPage_EmptyState_RendersStoryMessage()
    {
        await InitializeStoreAsync();

        IRenderedComponent<CounterPage> cut = Render<CounterPage>();

        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("No counter data yet. Send your first Increment Counter command.");
            cut.Markup.ShouldContain("Increment Counter");
        });
    }

    [Fact]
    public async Task CounterProjectionState_LoadActions_UpdateFluxorStateAndRegistryManifest()
    {
        ServiceCollection services = new();
        _ = services.AddFluentUIComponents();
        services.Replace(ServiceDescriptor.Scoped<IThemeService>(_ => Substitute.For<IThemeService>()));
        _ = services.AddHexalithFrontComposer(o => o.ScanAssemblies(typeof(CounterProjection).Assembly));
        _ = services.AddHexalithDomain<CounterDomain>();
        services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        await store.InitializeAsync();

        IFrontComposerRegistry registry = provider.GetRequiredService<IFrontComposerRegistry>();
        IState<CounterProjectionState> state = provider.GetRequiredService<IState<CounterProjectionState>>();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        string correlationId = Guid.NewGuid().ToString();
        dispatcher.Dispatch(new CounterProjectionLoadRequestedAction(correlationId));
        SpinWait.SpinUntil(() => state.Value.IsLoading, TimeSpan.FromSeconds(1)).ShouldBeTrue();

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            correlationId,
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 2,
                    LastUpdated = DateTimeOffset.UtcNow,
                },
            ]));

        SpinWait.SpinUntil(
            () => !state.Value.IsLoading && state.Value.Items?.Count == 1,
            TimeSpan.FromSeconds(1)).ShouldBeTrue();

        DomainManifest counterManifest = registry.GetManifests().Single(m => m.BoundedContext == "Counter");
        counterManifest.Projections.ShouldContain(typeof(CounterProjection).FullName!);
        counterManifest.Commands.ShouldContain(typeof(IncrementCommand).FullName!);
    }

    [Fact]
    public async Task CounterProjectionView_LoadedState_RendersColumnsAndFormatting()
    {
        UseFakeTime(s_fixedNow);

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        using CultureScope _ = new(CultureInfo.InvariantCulture);

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 1234,
                    LastUpdated = s_lastUpdated,
                },
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            string markup = cut.Markup;
            // Story 4-4 — Story 4.4's new envelope (data-fc-datagrid="...Counter..." attribute) introduces an
            // earlier "Count" substring inside the host div's data-* attribute. Anchor the column-header search
            // to the FluentDataGrid col-title-text marker so we keep the original ordering invariant.
            int idHeader = markup.IndexOf(">Id<", StringComparison.Ordinal);
            int countHeader = markup.IndexOf(">Count<", StringComparison.Ordinal);
            int lastUpdatedHeader = markup.IndexOf(">Last changed<", StringComparison.Ordinal);
            idHeader.ShouldBeGreaterThanOrEqualTo(0);
            countHeader.ShouldBeGreaterThanOrEqualTo(0);
            lastUpdatedHeader.ShouldBeGreaterThanOrEqualTo(0);
            idHeader.ShouldBeLessThan(countHeader);
            countHeader.ShouldBeLessThan(lastUpdatedHeader);
            markup.ShouldContain("counter-1");
            markup.ShouldContain("1,234");
            markup.ShouldContain("04/14/2026");
        });

        await Verify(NormalizeGridMarkup(cut.Markup));
    }

    [Fact]
    public async Task CounterProjectionView_SelectedTemplate_RendersInsideGridEnvelopeAndUsesFieldRenderer()
    {
        UseFakeTime(s_fixedNow);

        Services.AddSingleton(new ProjectionTemplateAssemblySource(
        [
            new ProjectionTemplateDescriptor(
                ProjectionType: typeof(CounterProjection),
                Role: null,
                TemplateType: typeof(SelectedCounterTemplate),
                ContractVersion: ProjectionTemplateContractVersion.Current),
        ]));

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        using CultureScope _ = new(CultureInfo.InvariantCulture);

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 1234,
                    LastUpdated = s_lastUpdated,
                },
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            string markup = cut.Markup;
            markup.ShouldContain("data-fc-datagrid");
            markup.ShouldContain("fc-selected-template");
            markup.ShouldContain("sections:2");
            markup.ShouldContain("1,234");
            markup.ShouldNotContain("fluent-data-grid");
        });
    }

    [Fact]
    public async Task CounterProjectionView_Level3Slot_ReplacesOneFieldAndLeavesAdjacentFieldsGenerated()
    {
        UseFakeTime(s_fixedNow);

        // GC-P4 — typed <TComponent> overload catches component-type mismatches at compile time.
        Services.AddSlotOverride<CounterProjection, int, CounterCountSlot>(field: x => x.Count);

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        using CultureScope _ = new(CultureInfo.InvariantCulture);

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 1234,
                    LastUpdated = s_lastUpdated,
                },
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            string markup = cut.Markup;

            // Slot replaces Count cell content.
            markup.ShouldContain("counter-count-slot");
            markup.ShouldContain("aria-label=\"Count: 1,234\"");

            // GC-P3 — DataGrid envelope and adjacent generated rendering preserved.
            markup.ShouldContain("data-fc-datagrid");
            markup.ShouldContain("counter-1");
            markup.ShouldContain("04/14/2026");

            // GC-P3 / GC-P8 — column headers (including the slot's own column) remain
            // generated. Slot replaces cell content only; header is part of grid metadata.
            markup.ShouldContain(">Id<");
            markup.ShouldContain(">Count<");
            markup.ShouldContain(">Last changed<");
        });

        // GC-P9 — visible-label invariant from Spec line 328 ("slot replacing a badge-like
        // field must preserve visible label text and equivalent accessible name"). Use a
        // DOM-anchored assertion rather than substring matching on the raw markup.
        AngleSharp.Dom.IElement labelElement = cut.Find(".counter-count-slot__label");
        labelElement.TextContent.Trim().ShouldBe("Count");

        // GC-P9 — exactly one slot rendered (one row × one slot field).
        cut.FindAll(".counter-count-slot").Count.ShouldBe(1);
    }

    [Fact]
    public async Task CounterProjectionView_Level3Slot_InvalidComponent_LogsHfc1039_AndRendersGeneratedDefault()
    {
        // GC-P1 / AC15 — invalid-registration test fixture. Component lacks a Context parameter,
        // so IsCompatibleComponent rejects it and HFC1039 is logged. Resolve returns null and
        // FcFieldSlotHost falls back to RenderDefault, so the cell still shows the generated
        // value "1,234" and no "counter-count-slot" markup appears.
        UseFakeTime(s_fixedNow);

        ListLogger<ProjectionSlotRegistry> capturedLogger = new();
        Services.Replace(ServiceDescriptor.Singleton<ILogger<ProjectionSlotRegistry>>(capturedLogger));

        Services.AddSlotOverride<CounterProjection, int>(
            field: x => x.Count,
            componentType: typeof(InvalidSlotMissingContext));

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        using CultureScope _ = new(CultureInfo.InvariantCulture);

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 1234,
                    LastUpdated = s_lastUpdated,
                },
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            string markup = cut.Markup;

            // Slot was REJECTED — no slot markup visible.
            markup.ShouldNotContain("counter-count-slot");
            markup.ShouldNotContain("INVALID-SLOT-RENDERED");

            // Generated default still renders the field.
            markup.ShouldContain("1,234");
            markup.ShouldContain(">Count<");
        });

        // HFC1039 fired with the canonical Expected/Got/Fix teaching shape.
        capturedLogger.Entries.ShouldContain(e =>
            e.Level == LogLevel.Warning
            && e.Message.Contains("HFC1039", StringComparison.Ordinal)
            && e.Message.Contains("Expected:", StringComparison.Ordinal)
            && e.Message.Contains("Got:", StringComparison.Ordinal)
            && e.Message.Contains("Fix:", StringComparison.Ordinal)
            && e.Message.Contains(typeof(CounterProjection).FullName!, StringComparison.Ordinal)
            && e.Message.Contains(typeof(InvalidSlotMissingContext).FullName!, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CounterProjectionView_Level2TemplateAndLevel3Slot_TemplateFieldRendererResolvesSlot()
    {
        // GC-P2 / AC10 / T6 spec line 122 — "tests proving a Level 2 template still renders a
        // Level 3 slot for one field and default generated delegates for adjacent fields".
        UseFakeTime(s_fixedNow);

        Services.AddSingleton(new ProjectionTemplateAssemblySource(
        [
            new ProjectionTemplateDescriptor(
                ProjectionType: typeof(CounterProjection),
                Role: null,
                TemplateType: typeof(SelectedCounterTemplate),
                ContractVersion: ProjectionTemplateContractVersion.Current),
        ]));

        Services.AddSlotOverride<CounterProjection, int, CounterCountSlot>(field: x => x.Count);

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        using CultureScope _ = new(CultureInfo.InvariantCulture);

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 1234,
                    LastUpdated = s_lastUpdated,
                },
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            string markup = cut.Markup;

            // Level 2 template owns the body — selected-template marker present, no FluentDataGrid.
            markup.ShouldContain("fc-selected-template");
            markup.ShouldNotContain("fluent-data-grid");

            // Level 2 template's FieldRenderer invocation for "Count" resolves through the
            // Level 3 slot per spec line 281 ("the helper checks a Level 3 slot descriptor for
            // (projection, role, field) and falls back to ... generated default field renderer").
            markup.ShouldContain("counter-count-slot");
            markup.ShouldContain("aria-label=\"Count: 1,234\"");
        });
    }

    [Fact]
    public async Task CounterProjectionView_Level4Replacement_RendersInsideFrameworkEnvelope_AndUsesSafeFieldDelegates()
    {
        UseFakeTime(s_fixedNow);

        Services.AddViewOverride<CounterProjection, CounterFullViewReplacement>();
        Services.AddSlotOverride<CounterProjection, int, CounterCountSlot>(field: x => x.Count);

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        using CultureScope _ = new(CultureInfo.InvariantCulture);

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 1234,
                    LastUpdated = s_lastUpdated,
                },
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            string markup = cut.Markup;

            // Framework-owned envelope remains outside the replacement.
            markup.ShouldContain("data-fc-datagrid");
            markup.ShouldContain("counter-full-view-heading");

            // Level 4 wins over generated DataGrid body by default.
            markup.ShouldNotContain("fluent-data-grid");

            // Lower-level rendering appears only through explicit safe delegates used by the
            // replacement. Count flows through the Level 3 slot; LastUpdated uses generated field rendering.
            markup.ShouldContain("counter-count-slot");
            markup.ShouldContain("aria-label=\"Count: 1,234\"");
            markup.ShouldContain("04/14/2026");
        });
    }

    [Fact]
    public async Task CounterProjectionView_Level4InvalidComponent_LogsHfc1043_AndRendersGeneratedDefault()
    {
        UseFakeTime(s_fixedNow);

        ListLogger<ProjectionViewOverrideRegistry> capturedLogger = new();
        Services.Replace(ServiceDescriptor.Singleton<ILogger<ProjectionViewOverrideRegistry>>(capturedLogger));
        Services.AddViewOverride<CounterProjection, InvalidViewMissingContext>();

        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        using CultureScope _ = new(CultureInfo.InvariantCulture);

        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new CounterProjection
                {
                    Id = "counter-1",
                    Count = 1234,
                    LastUpdated = s_lastUpdated,
                },
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            string markup = cut.Markup;
            markup.ShouldContain("fluent-data-grid");
            markup.ShouldContain("1,234");
            markup.ShouldNotContain("INVALID-VIEW-RENDERED");
        });

        capturedLogger.Entries.ShouldContain(e =>
            e.Level == LogLevel.Warning
            && e.Message.Contains("HFC1043", StringComparison.Ordinal)
            && e.Message.Contains("Expected:", StringComparison.Ordinal)
            && e.Message.Contains("Got:", StringComparison.Ordinal)
            && e.Message.Contains("Fix:", StringComparison.Ordinal)
            && e.Message.Contains(typeof(CounterProjection).FullName!, StringComparison.Ordinal)
            && e.Message.Contains(typeof(InvalidViewMissingContext).FullName!, StringComparison.Ordinal));
    }

    [Fact]
    public async Task StatusProjectionView_NullAndBooleanValues_RenderSnapshot()
    {
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new StatusProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                new StatusProjection { Name = null, IsEnabled = true },
                new StatusProjection { Name = "Beta", IsEnabled = false },
                new StatusProjection { Name = "Gamma", IsEnabled = null },
            ]));

        IRenderedComponent<StatusProjectionView> cut = Render<StatusProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("Yes");
            cut.Markup.ShouldContain("No");
            cut.Markup.ShouldContain("—");
        });

        _ = await Verify(NormalizeGridMarkup(cut.Markup));
    }

    private void UseFakeTime(DateTimeOffset utcNow)
    {
        FakeTimeProvider fake = new(utcNow);
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(fake));
    }

    private static string NormalizeGridMarkup(string markup)
    {
        string normalized = Regex.Replace(markup, "\\s+id=\"[^\"]+\"", string.Empty);
        normalized = Regex.Replace(normalized, "\\s+blazor:[^=]+=\"[^\"]*\"", string.Empty);
        // Story 4-6 review fix: per-instance Guid suffix on _expandPanelId (added to prevent
        // duplicate DOM ids) makes aria-controls non-deterministic across runs.
        // P-6 (Pass-3): narrow the scrub to the specific `fc-expand-panel-{viewKey}-{guid32}`
        // slot so future emitter additions of any 32-hex sequence (e.g., a content hash) are
        // NOT silently masked.
        normalized = Regex.Replace(normalized, @"fc-expand-panel-([A-Za-z0-9-]+)-[0-9a-f]{32}", "fc-expand-panel-$1-{guid}");
        return normalized.Replace("\r\n", "\n");
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;

        public CultureScope(CultureInfo culture)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }

    private sealed class SelectedCounterTemplate : ComponentBase
    {
        [Parameter]
        public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            CounterProjection row = Context.Items[0];
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "fc-selected-template");
            builder.AddContent(2, "sections:" + Context.Sections.Count.ToString(CultureInfo.InvariantCulture));
            builder.AddContent(3, Context.FieldRenderer(row, nameof(CounterProjection.Count)));
            builder.CloseElement();
        }
    }

    // GC-P1 — invalid Level 3 slot fixture: lacks the required [Parameter] Context property.
    // Triggers IsCompatibleComponent rejection → HFC1039 → fail-soft to default rendering.
    private sealed class InvalidSlotMissingContext : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "invalid-slot-marker");
            builder.AddContent(2, "INVALID-SLOT-RENDERED");
            builder.CloseElement();
        }
    }

    private sealed class InvalidViewMissingContext : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "INVALID-VIEW-RENDERED");
            builder.CloseElement();
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
