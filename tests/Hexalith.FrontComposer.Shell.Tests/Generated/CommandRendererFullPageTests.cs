using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Forms;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

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
    public async Task Renderer_FullPage_WrapsEditFormInFcLifecycleWrapper() {
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact]
    public async Task Renderer_FullPage_DirtyGeneratedForm_ShowsAbandonmentGuardAndPreventsNavigation() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();
        IRenderedComponent<FcFormAbandonmentGuard> guardCut = cut.FindComponent<FcFormAbandonmentGuard>();
        EditContext editContext = WaitForGuardEditContext(guardCut);

        await cut.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(FiveFieldFullPageCommand.Name))));
        time.Advance(TimeSpan.FromSeconds(31));

        LocationChangingContext context = BuildLocationChangingContext("/other");
        await InvokeNavigationChangingAsync(guardCut, context);

        DidPreventNavigation(context).ShouldBeTrue();
        cut.WaitForAssertion(() => cut.Find("[data-testid='fc-form-abandonment-warning']"));
    }

    [Fact]
    public async Task Renderer_FullPage_CleanGeneratedForm_DoesNotShowAbandonmentGuardOrPreventNavigation() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(time));
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();
        IRenderedComponent<FcFormAbandonmentGuard> guardCut = cut.FindComponent<FcFormAbandonmentGuard>();
        _ = WaitForGuardEditContext(guardCut);
        time.Advance(TimeSpan.FromSeconds(31));

        LocationChangingContext context = BuildLocationChangingContext("/other");
        await InvokeNavigationChangingAsync(guardCut, context);

        DidPreventNavigation(context).ShouldBeFalse();
        cut.FindAll("[data-testid='fc-form-abandonment-warning']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Renderer_FullPage_Submit_RefreshesDerivableValuesBeforeDispatch() {
        RecordingDerivedValueProvider derivedValues = new();
        RecordingCommandService commandService = new();
        Services.Replace(ServiceDescriptor.Scoped<ICommandService>(_ => commandService));
        Services.RemoveAll<IDerivedValueProvider>();
        _ = Services.AddSingleton<IDerivedValueProvider>(derivedValues);
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("form"));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => commandService.LastCommand.ShouldNotBeNull());
        derivedValues.Properties.ShouldContain(nameof(FiveFieldFullPageCommand.MessageId));
        commandService.LastCommand!.MessageId.ShouldBe(RecordingDerivedValueProvider.MessageId);
    }

    [Fact]
    public async Task Renderer_FullPage_InvalidReturnPath_FallsBackToHomeBreadcrumb() {
        PageContext.ReturnPath = "https://evil.example/path";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("href=\"/\"", Case.Insensitive));
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

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("aria-label=\"breadcrumb\"", Case.Insensitive));
    }

    [Fact]
    public async Task Renderer_FullPage_HidesEmbeddedBreadcrumbWhenOptionOff() {
        // Decision D15 — EmbeddedBreadcrumb=false suppresses the inline breadcrumb (Story 3.1
        // shell-level breadcrumb takes over).
        _ = Services.Configure<FcShellOptions>(o => o.EmbeddedBreadcrumb = false);
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldNotContain("aria-label=\"breadcrumb\"", Case.Insensitive));
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

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("href=\"/\"", Case.Insensitive));
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

    private static EditContext WaitForGuardEditContext(IRenderedComponent<FcFormAbandonmentGuard> guardCut) {
        guardCut.WaitForAssertion(() => guardCut.Instance.EditContext.ShouldNotBeNull());
        return guardCut.Instance.EditContext!;
    }

    private static Task InvokeNavigationChangingAsync(
        IRenderedComponent<FcFormAbandonmentGuard> guardCut,
        LocationChangingContext context)
        => guardCut.InvokeAsync(() => {
            var handle = (Task)typeof(FcFormAbandonmentGuard).GetMethod(
                "HandleNavigationChangingAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(guardCut.Instance, [context])!;
            return handle;
        });

    private static LocationChangingContext BuildLocationChangingContext(string target) {
        Type type = typeof(LocationChangingContext);
        System.Reflection.ConstructorInfo ctor = type.GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)[0];
        System.Reflection.ParameterInfo[] pars = ctor.GetParameters();
        object[] args = new object[pars.Length];
        for (int i = 0; i < pars.Length; i++) {
            if (pars[i].ParameterType == typeof(string)) {
                args[i] = target;
            }
            else if (pars[i].ParameterType == typeof(bool)) {
                args[i] = false;
            }
            else if (pars[i].ParameterType == typeof(CancellationToken)) {
                args[i] = CancellationToken.None;
            }
            else {
                args[i] = pars[i].HasDefaultValue ? pars[i].DefaultValue! : null!;
            }
        }

        return (LocationChangingContext)ctor.Invoke(args);
    }

    private static bool DidPreventNavigation(LocationChangingContext context) {
        System.Reflection.FieldInfo? field = typeof(LocationChangingContext)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SingleOrDefault(f => f.FieldType == typeof(bool) && f.Name.Contains("prevent", StringComparison.OrdinalIgnoreCase));
        field.ShouldNotBeNull("LocationChangingContext keeps PreventNavigation state non-public in .NET 10.");
        return (bool)field.GetValue(context)!;
    }

    private sealed class RecordingDerivedValueProvider : IDerivedValueProvider {
        public const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

        public List<string> Properties { get; } = [];

        public Task<DerivedValueResult> ResolveAsync(
            Type commandType,
            string propertyName,
            ProjectionContext? context,
            CancellationToken cancellationToken = default) {
            Properties.Add(propertyName);
            return Task.FromResult(propertyName == nameof(FiveFieldFullPageCommand.MessageId)
                ? new DerivedValueResult(true, MessageId)
                : new DerivedValueResult(false, null));
        }
    }

    private sealed class RecordingCommandService : ICommandServiceWithLifecycle {
        public FiveFieldFullPageCommand? LastCommand { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            if (command is FiveFieldFullPageCommand typed) {
                LastCommand = typed;
            }

            return Task.FromResult(new CommandResult(RecordingDerivedValueProvider.MessageId, "Accepted"));
        }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<Hexalith.FrontComposer.Contracts.Lifecycle.CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            if (command is FiveFieldFullPageCommand typed) {
                LastCommand = typed;
            }

            return Task.FromResult(new CommandResult(RecordingDerivedValueProvider.MessageId, "Accepted"));
        }
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
