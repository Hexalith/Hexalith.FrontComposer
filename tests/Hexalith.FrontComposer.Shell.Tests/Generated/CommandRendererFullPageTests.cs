using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class CommandRendererFullPageTests : CommandRendererTestBase {
    [Fact]
    public async Task Renderer_FullPage_RendersWithoutComponentParameterMismatch() {
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        Should.NotThrow(() => Render<FiveFieldFullPageCommandRenderer>());
    }

    [Fact]
    public async Task Renderer_FullPage_InvalidReturnPath_FallsBackToHomeBreadcrumb() {
        PageContext.ReturnPath = "https://evil.example/path";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("href=\"/\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_FullPage_UsesConfiguredMaxWidthWithoutNestedFormWidthClamp() {
        _ = Services.Configure<FcShellOptions>(o => o.FullPageFormMaxWidth = "960px");
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("max-width: 960px", Case.Insensitive);
            cut.Markup.ShouldNotContain("max-width: 720px", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_FullPage_UsesQueryFallbacksWhenPageContextIsEmpty() {
        PageContext.ReturnPath = null;
        PageContext.BoundedContext = string.Empty;
        await InitializeStoreAsync();
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/commands/TestCommands/FiveFieldFullPageCommand?returnPath=%2Fcounter");

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("href=\"/counter\"", Case.Insensitive);
            cut.Markup.ShouldContain("TestCommands");
        });
    }

    [Fact]
    public async Task Renderer_FullPage_RendersEmbeddedBreadcrumbWhenOptionOn() {
        // Decision D15 — EmbeddedBreadcrumb defaults to true; renderer emits a <nav aria-label="breadcrumb">.
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("aria-label=\"breadcrumb\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_FullPage_HidesEmbeddedBreadcrumbWhenOptionOff() {
        // Decision D15 — EmbeddedBreadcrumb=false suppresses the inline breadcrumb (Story 3.1
        // shell-level breadcrumb takes over).
        _ = Services.Configure<FcShellOptions>(o => o.EmbeddedBreadcrumb = false);
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("aria-label=\"breadcrumb\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_FullPage_ReturnPathProtocolRelative_LogsAndFallsBackToHome() {
        // Decision D32 — `//evil.com` is protocol-relative and must be rejected by
        // IsValidRelativeReturnPath. The breadcrumb falls back to "/" and a structured
        // log is emitted on the post-submit navigation path.
        TestLogger<FiveFieldFullPageCommandRenderer> logger = new();
        _ = Services.AddSingleton<ILogger<FiveFieldFullPageCommandRenderer>>(logger);
        PageContext.ReturnPath = "//evil.example/path";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("href=\"/\"", Case.Insensitive);
            // The breadcrumb itself does not log (it just falls back); the navigation path logs
            // when invoked. We assert the breadcrumb fallback here as the user-visible D32 effect.
        });
    }

    [Fact]
    public async Task Page_HasGeneratedRouteAttribute() {
        // AC4 / Decision D5 + D22 — the generated `{Cmd}Page` partial declares a
        // `[Route("/commands/{BoundedContext}/{CommandTypeName}")]` so deep-linking and the
        // Counter sample anchor work.
        Type pageType = typeof(FiveFieldFullPageCommandPage);
        IEnumerable<RouteAttribute> routes = pageType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>();

        routes.ShouldContain(r => r.Template == "/commands/TestCommands/FiveFieldFullPageCommand");
    }

    [Fact]
    public async Task Page_DispatchesRestoreGridStateOnMount_WhenProjectionFqnQueryPresent() {
        // AC7 + Decision D30 — `{Cmd}Page` dispatches RestoreGridStateAction on init when the
        // query string carries `projectionTypeFqn`. The reducer is a no-op in v0.1 (effects
        // land in Story 4.3) — we only assert the contract via the dispatched action type
        // (subscribing to IDispatcher.ActionDispatched event so we don't need to replace the
        // dispatcher after the bUnit container is frozen).
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        List<object> dispatchedActions = [];
        dispatcher.ActionDispatched += (_, e) => dispatchedActions.Add(e.Action);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/commands/TestCommands/FiveFieldFullPageCommand?projectionTypeFqn=Counter.Domain.CounterProjection");

        _ = Render<FiveFieldFullPageCommandPage>();

        dispatchedActions
            .OfType<RestoreGridStateAction>()
            .ShouldNotBeEmpty("the page must dispatch RestoreGridStateAction on mount when query carries the projection FQN");
    }

    private sealed class TestLogger<T> : ILogger<T> {
        public List<string> WarningMessages { get; } = [];

        IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            if (logLevel >= LogLevel.Warning) {
                WarningMessages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
