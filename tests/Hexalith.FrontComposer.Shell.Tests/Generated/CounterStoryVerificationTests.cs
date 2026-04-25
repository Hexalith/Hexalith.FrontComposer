using System.Globalization;
using System.Text.RegularExpressions;

using Bunit;

using Counter.Domain;
using Counter.Web.Components.Pages;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class CounterStoryVerificationTests : GeneratedComponentTestBase
{

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
                    LastUpdated = new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
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
            int lastUpdatedHeader = markup.IndexOf(">Last Updated<", StringComparison.Ordinal);
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
}
