using Bunit;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.ProjectionTemplates;
using Hexalith.FrontComposer.Shell.Components.Diagnostics;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using NSubstitute;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcProjectionViewOverrideHostTests : BunitContext {
    private readonly ListLogger<FcProjectionViewOverrideHost<ViewProjection>> _logger = new();

    public FcProjectionViewOverrideHostTests() {
        // P1 / P2 — the diagnostic panel injects IStringLocalizer<FcShellResources> and uses
        // Fluent UI primitives (FluentMessageBar / FluentButton). Register localization +
        // Fluent UI services so the panel can render in the test bUnit context.
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        Services.AddSingleton(environment);
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLocalization();
        Services.Replace(ServiceDescriptor.Singleton<ILogger<FcProjectionViewOverrideHost<ViewProjection>>>(_logger));
    }

    [Fact]
    public void Render_ValidReplacement_PassesItemsThrough() {
        ProjectionViewContext<ViewProjection> context = NewContext(
            [new ViewProjection(7, "Alpha"), new ViewProjection(8, "Beta")]);
        ProjectionViewOverrideDescriptor descriptor = NewDescriptor(typeof(EchoReplacement));

        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, context));

        cut.Markup.ShouldContain("override-count=\"2\"");
        cut.Markup.ShouldContain("Alpha");
        cut.Markup.ShouldContain("Beta");
    }

    [Fact]
    public void Render_RepeatedRendersWithDifferentItems_PassFreshContext() {
        // P2 — replaces the previously tautological count="1" assertion with a real
        // freshness check rotating items, tenant, and user across two renders.
        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(EchoReplacement)))
            .Add(p => p.Context, NewContext([new ViewProjection(1, "First")], tenant: "tenant-a", user: "user-a")));

        cut.Markup.ShouldContain("override-count=\"1\"");
        cut.Markup.ShouldContain("First");

        cut.Render(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(EchoReplacement)))
            .Add(p => p.Context, NewContext([new ViewProjection(2, "Second"), new ViewProjection(3, "Third")], tenant: "tenant-b", user: "user-b")));

        cut.Markup.ShouldContain("override-count=\"2\"");
        cut.Markup.ShouldContain("Second");
        cut.Markup.ShouldContain("Third");
        cut.Markup.ShouldNotContain("First");
    }

    [Fact]
    public void Render_ThrowingReplacement_IsolatesFault_AndRendersDiagnosticFallback() {
        InMemoryDiagnosticSink sink = new(capacity: 4);
        Services.AddSingleton<IDiagnosticSink>(sink);
        ProjectionViewContext<ViewProjection> context = NewContext([new ViewProjection(7, "PayloadValueMustNotLog")]);
        ProjectionViewOverrideDescriptor descriptor = NewDescriptor(typeof(ThrowingReplacement));

        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, context));

        AssertDiagnosticVisibility(cut.Markup, "role=\"alert\"");
        AssertDiagnosticVisibility(cut.Markup, FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);

        // P1 — replaced the always-true Exception==null assertion with positive checks: the
        // log message MUST contain HFC2121, the projection field "PayloadValueMustNotLog"
        // MUST NOT appear, the raw exception text MUST NOT appear, and tenant/user values
        // MUST NOT appear (only their hashes).
        var entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.Id.ShouldBe(5812);
        entry.EventId.Name.ShouldBe("ProjectionViewOverrideRenderFailed");
        entry.Exception.ShouldBeNull();
        entry.Message.ShouldContain(FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
        entry.Message.ShouldNotContain("PayloadValueMustNotLog");
        entry.Message.ShouldNotContain("raw replacement message");
        entry.Message.ShouldContain("TenantDigest=sha256:");
        entry.Message.ShouldContain("UserDigest=sha256:");

#if DEBUG
        DevDiagnosticEvent evt = sink.RecentEvents.ShouldHaveSingleItem();
        evt.Code.ShouldBe(FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
        evt.Message.ShouldContain("What:");
        evt.Message.ShouldContain("Expected:");
        evt.Message.ShouldContain("Got:");
        evt.Message.ShouldContain("Fix:");
        evt.Message.ShouldContain("DocsLink:");
        evt.Message.ShouldNotContain("PayloadValueMustNotLog");
        evt.Message.ShouldNotContain("raw replacement message");
        evt.Message.ShouldNotContain("tenant");
        evt.Message.ShouldNotContain("user");
#else
        sink.RecentEvents.ShouldBeEmpty();
#endif
    }

    [Fact]
    public void Render_DescriptorChange_RecoversBoundary() {
        // P7 — error-boundary recovery on descriptor change. AC8 / T5 require the host to
        // recover when the selected descriptor changes (e.g., adopter swaps a faulty
        // replacement for a corrected one) without a full shell reload.
        ProjectionViewContext<ViewProjection> context = NewContext([new ViewProjection(1, "Item")]);

        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(ThrowingReplacement)))
            .Add(p => p.Context, context));

        AssertDiagnosticVisibility(cut.Markup, FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);

        cut.Render(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(EchoReplacement)))
            .Add(p => p.Context, context));

        cut.Markup.ShouldContain("override-count=\"1\"");
        cut.Markup.ShouldContain("Item");
        cut.Markup.ShouldNotContain(FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
    }

    [Fact]
    public void Render_PersistentlyThrowingReplacement_DoesNotLogPerItemsTick() {
        // DN2 — Items churn on every Fluxor tick must not flood HFC2121 logs. Recovery is
        // keyed on Descriptor + RenderContext only; rotating Items between renders should
        // NOT cause additional HFC2121 entries beyond the first failure.
        ProjectionViewOverrideDescriptor descriptor = NewDescriptor(typeof(ThrowingReplacement));

        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, NewContext([new ViewProjection(1, "v1")])));

        int logsAfterFirstRender = _logger.Entries.Count;
        logsAfterFirstRender.ShouldBe(1);

        // 5 Items-only re-renders with same RenderContext. Each produces a NEW IReadOnlyList
        // reference — under the pre-DN2 reference-equals comparison this would call
        // _boundary.Recover() five times and emit five HFC2121 lines. Post-DN2 the boundary
        // stays in error mode.
        for (int i = 2; i <= 6; i++) {
            int iteration = i;
            cut.Render(parameters => parameters
                .Add(p => p.Descriptor, descriptor)
                .Add(p => p.Context, NewContext([new ViewProjection(iteration, $"v{iteration}")])));
        }

        // Items-only churn must not produce additional HFC2121 entries.
        _logger.Entries.Count.ShouldBe(logsAfterFirstRender);
    }

    [Fact]
    public void Render_NullContextOnRerender_DoesNotThrow() {
        // P3 — OnParametersSet must not NRE when a regression nulls Context after an
        // initial successful render.
        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(EchoReplacement)))
            .Add(p => p.Context, NewContext([new ViewProjection(1, "Alpha")])));

        Should.NotThrow(() => cut.Render(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(EchoReplacement)))
            .Add(p => p.Context, null!)));
    }

    [Fact]
    public void Render_AccessibilityContract_NoFluentFocusOverrideAndStateAnnouncementsAreAssertive() {
        // P8 — accessibility oracles for the sample host. The host wrapper renders no custom
        // CSS today (CSS isolation files do not exist for this component), so focus visibility
        // and reduced-motion / forced-colors are vacuously satisfied. State announcements emit
        // a `role="alert"` diagnostic fallback, which is the assertive category for critical
        // failures (matches framework lifecycle policy in FcLifecycleWrapper).
        ProjectionViewContext<ViewProjection> context = NewContext([new ViewProjection(1, "Alpha")]);

        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(ThrowingReplacement)))
            .Add(p => p.Context, context));

        // Diagnostic fallback uses role="alert" (assertive) which matches framework critical
        // state announcements. Replacement bodies own non-critical aria-live policy.
        AssertDiagnosticVisibility(cut.Markup, "role=\"alert\"");
        // Host emits no CSS; no --colorStrokeFocus2 override is possible at this layer.
        cut.Markup.ShouldNotContain("--colorStrokeFocus2");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData(null)]
    public void Render_Fault_PreservesGeneratedBodyAndFailsClosedForDiagnostics(string? environmentName)
    {
        Services.RemoveAll<IHostEnvironment>();
        if (environmentName is not null)
        {
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            Services.AddSingleton(environment);
        }
        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(ThrowingReplacement)))
            .Add(p => p.Context, NewContext([new ViewProjection(7, "SensitivePayloadSentinel")], "SensitiveTenantSentinel", "SensitiveUserSentinel")));
        cut.Markup.ShouldContain("Generated fallback");
        bool visible = false;
#if DEBUG
        visible = environmentName == "Development";
#endif
        cut.FindAll("[data-fc-diagnostic]").Count.ShouldBe(visible ? 1 : 0);
        cut.Markup.ShouldNotContain("SensitivePayloadSentinel");
        cut.Markup.ShouldNotContain("SensitiveTenantSentinel");
        cut.Markup.ShouldNotContain("SensitiveUserSentinel");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Render_Level4Fault_TriesCompatibleTemplateThenGeneratedBody(bool templateThrows, bool incompatible)
    {
        ProjectionTemplateRegistry registry = new(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectionTemplateRegistry>.Instance,
            [new ProjectionTemplateAssemblySource([new ProjectionTemplateDescriptor(typeof(ViewProjection), null,
                templateThrows ? typeof(ThrowingFallbackTemplate) : typeof(FallbackTemplate),
                incompatible ? 999 : ProjectionTemplateContractVersion.Current)])]);
        Services.AddSingleton<IProjectionTemplateRegistry>(registry);
        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(ThrowingReplacement)))
            .Add(p => p.Context, NewContext([new ViewProjection(7, "SensitivePayloadSentinel")])));
        cut.Markup.ShouldContain(templateThrows || incompatible ? "Generated fallback" : "Level 2 fallback");
        cut.Markup.ShouldNotContain("SensitivePayloadSentinel");
        _logger.Entries.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData(null)]
    public void Render_DirectDiagnosticPanel_RequiresDebugAndExplicitDevelopment(string? environmentName)
    {
        Services.RemoveAll<IHostEnvironment>();
        if (environmentName is not null)
        {
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            Services.AddSingleton(environment);
        }
        CustomizationDiagnostic diagnostic = CustomizationDiagnostic.Create(
            "HFC2121", CustomizationDiagnosticSeverity.Warning, CustomizationDiagnosticPhase.Runtime,
            CustomizationLevel.Level4, "Projection", "Component", null, null,
            "Render fault", "Usable body", "RenderFault", "Repair replacement", "Generated fallback",
            "https://hexalith.github.io/FrontComposer/diagnostics/HFC2121");
        IRenderedComponent<FcCustomizationDiagnosticPanel> cut = Render<FcCustomizationDiagnosticPanel>(parameters => parameters
            .Add(p => p.Diagnostic, diagnostic));
        bool visible = false;
#if DEBUG
        visible = environmentName == "Development";
#endif
        cut.FindAll("[data-fc-diagnostic]").Count.ShouldBe(visible ? 1 : 0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Render_ThrowingGeneratedFallback_IsContainedWithoutRecursiveRecovery(bool childThrows, bool templateThrows)
    {
        IErrorBoundaryLogger boundaryLogger = Substitute.For<IErrorBoundaryLogger>();
        Services.AddSingleton(boundaryLogger);
        InMemoryDiagnosticSink sink = new(capacity: 8);
        Services.AddSingleton<IDiagnosticSink>(sink);
        if (templateThrows)
        {
            ProjectionTemplateRegistry registry = new(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ProjectionTemplateRegistry>.Instance,
                [new ProjectionTemplateAssemblySource([new ProjectionTemplateDescriptor(typeof(ViewProjection), null,
                    typeof(ThrowingFallbackTemplate), ProjectionTemplateContractVersion.Current)])]);
            Services.AddSingleton<IProjectionTemplateRegistry>(registry);
        }
        int attempts = 0;
        RenderFragment fallback = builder =>
        {
            attempts++;
            if (!childThrows)
            {
                throw new InvalidOperationException("SensitiveFallbackPayload token stack trace");
            }
            builder.OpenComponent<ThrowingGeneratedFallback>(0);
            builder.CloseComponent();
        };
        ProjectionViewContext<ViewProjection> context = NewContext([new ViewProjection(7, "SensitiveRow")], defaultBody: fallback);
        ProjectionViewOverrideDescriptor descriptor = NewDescriptor(typeof(ThrowingReplacement));
        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor).Add(p => p.Context, context));
        cut.Render(parameters => parameters.Add(p => p.Descriptor, descriptor).Add(p => p.Context, context));
        attempts.ShouldBe(1);
        cut.Markup.ShouldContain("Data could not be loaded.");
        cut.Markup.ShouldNotContain("SensitiveFallbackPayload");
        cut.Markup.ShouldNotContain("SensitiveRow");
        _logger.Entries.ShouldHaveSingleItem().Message.ShouldNotContain("SensitiveFallbackPayload");
        await boundaryLogger.Received(templateThrows ? 2 : 1).LogErrorAsync(Arg.Any<Exception>()).ConfigureAwait(true);
        await boundaryLogger.DidNotReceive().LogErrorAsync(Arg.Is<Exception>(error => error.Message.Contains("SensitiveFallbackPayload", StringComparison.Ordinal))).ConfigureAwait(true);
        sink.RecentEvents.Count.ShouldBe(ExpectedDiagnosticCount * (templateThrows ? 2 : 1));
        sink.RecentEvents.ShouldAllBe(entry => !entry.Message.Contains("SensitiveFallbackPayload", StringComparison.Ordinal));
    }

    private static int ExpectedDiagnosticCount
    {
        get
        {
#if DEBUG
            return 1;
#else
            return 0;
#endif
        }
    }

    private static void AssertDiagnosticVisibility(string markup, string diagnostic)
    {
#if DEBUG
        markup.ShouldContain(diagnostic);
#else
        markup.ShouldNotContain(diagnostic);
#endif
    }

    private static ProjectionViewOverrideDescriptor NewDescriptor(Type componentType)
        => new(
            typeof(ViewProjection),
            null,
            componentType,
            ProjectionViewOverrideContractVersion.Current,
            "test");

    private static ProjectionViewContext<ViewProjection> NewContext(
        IReadOnlyList<ViewProjection> items,
        string tenant = "tenant",
        string user = "user",
        RenderFragment? defaultBody = null)
        => new(
            projectionType: typeof(ViewProjection),
            boundedContext: "Tests",
            role: null,
            items: items,
            renderContext: new RenderContext(tenant, user, FcRenderMode.Server, DensityLevel.Comfortable, IsReadOnly: false),
            columns: [new ProjectionTemplateColumnDescriptor("Name", "Name", null, null)],
            sections: [new ProjectionTemplateSectionDescriptor("Body", "Body", "Body")],
            lifecycleState: "Loaded",
            entityLabel: "Projection",
            entityPluralLabel: "Projections",
            defaultBody: defaultBody ?? (static builder => builder.AddContent(0, "Generated fallback")),
            sectionRenderer: static _ => static _ => { },
            rowRenderer: static _ => static _ => { },
            fieldRenderer: static (_, _) => static _ => { });

    public sealed record ViewProjection(int Id, string Name);

    public sealed class EchoReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            ArgumentNullException.ThrowIfNull(builder);
            builder.OpenElement(0, "section");
            builder.AddAttribute(1, "override-count", Context.Items.Count);
            foreach (ViewProjection item in Context.Items) {
                builder.AddContent(2, item.Name);
                builder.AddMarkupContent(3, " ");
            }

            builder.CloseElement();
        }
    }

    public sealed class ThrowingReplacement : ComponentBase {
        [Parameter]
        public ProjectionViewContext<ViewProjection> Context { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            => throw new InvalidOperationException("raw replacement message");
    }

    private sealed class ListLogger<T> : ILogger<T> {
        public List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception), exception));

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
