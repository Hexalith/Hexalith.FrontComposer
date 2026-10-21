using Bunit;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using NSubstitute;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 6-6 P18 — bUnit boundary sibling-survival + redaction tests for the Level 2
/// <see cref="FcProjectionTemplateHost{TProjection}"/>. Mirrors the Level 4 fixture pattern
/// in <c>FcProjectionViewOverrideHostTests</c>.
/// </summary>
public sealed class FcProjectionTemplateHostTests : BunitContext {
    private readonly ListLogger<FcProjectionTemplateHost<TemplateProjection>> _logger = new();

    public FcProjectionTemplateHostTests() {
        // P1 / P2 — diagnostic panel injects IStringLocalizer<FcShellResources> and uses
        // Fluent UI primitives. Register localization + Fluent UI services so the panel
        // can render in the test bUnit context.
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        Services.AddSingleton(environment);
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLocalization();
        Services.Replace(ServiceDescriptor.Singleton<ILogger<FcProjectionTemplateHost<TemplateProjection>>>(_logger));
    }

    [Fact]
    public void Render_ValidTemplate_PassesContextThrough() {
        ProjectionTemplateContext<TemplateProjection> context = NewContext("Hello");
        ProjectionTemplateDescriptor descriptor = NewDescriptor(typeof(EchoTemplate));

        IRenderedComponent<FcProjectionTemplateHost<TemplateProjection>> cut = Render<FcProjectionTemplateHost<TemplateProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, context));

        cut.Markup.ShouldContain("template-greeting=\"Hello\"");
    }

    [Fact]
    public void Render_ThrowingTemplate_IsolatesFault_DoesNotLeakItemPayloadOrException() {
        // P7 — Level 2 redaction adversarial test. The template throws with sensitive-looking
        // text and the projection carries a value that must not appear in panel output or logs.
        InMemoryDiagnosticSink sink = new(capacity: 4);
        Services.AddSingleton<IDiagnosticSink>(sink);
        ProjectionTemplateContext<TemplateProjection> context = NewContext("PayloadValueMustNotLog");
        ProjectionTemplateDescriptor descriptor = NewDescriptor(typeof(ThrowingTemplate));

        IRenderedComponent<FcProjectionTemplateHost<TemplateProjection>> cut = Render<FcProjectionTemplateHost<TemplateProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, context));

        AssertDiagnosticVisibility(cut.Markup, "role=\"alert\"");
        AssertDiagnosticVisibility(cut.Markup, FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault);

        (LogLevel Level, string Message, Exception? Exception) entry = _logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldContain(FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault);
        entry.Message.ShouldNotContain("PayloadValueMustNotLog");
        entry.Message.ShouldNotContain("raw template exception");

#if DEBUG
        DevDiagnosticEvent evt = sink.RecentEvents.ShouldHaveSingleItem();
        evt.Code.ShouldBe(FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault);
        evt.Message.ShouldContain("What:");
        evt.Message.ShouldContain("Expected:");
        evt.Message.ShouldContain("Got:");
        evt.Message.ShouldContain("Fix:");
        evt.Message.ShouldContain("DocsLink:");
        evt.Message.ShouldNotContain("PayloadValueMustNotLog");
        evt.Message.ShouldNotContain("raw template exception");
#else
        sink.RecentEvents.ShouldBeEmpty();
#endif
    }

    [Fact]
    public void Render_ThrowingTemplate_PublishesDiagnosticOnce_OnRepeatedRenders() {
        // P18 — publish-once on repeated re-renders within a single fault episode.
        InMemoryDiagnosticSink sink = new(capacity: 8);
        Services.AddSingleton<IDiagnosticSink>(sink);
        ProjectionTemplateDescriptor descriptor = NewDescriptor(typeof(ThrowingTemplate));

        IRenderedComponent<FcProjectionTemplateHost<TemplateProjection>> cut = Render<FcProjectionTemplateHost<TemplateProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, NewContext("v1")));

        sink.RecentEvents.Count.ShouldBe(ExpectedDiagnosticCount);

        for (int i = 2; i <= 5; i++) {
            int iteration = i;
            cut.Render(parameters => parameters
                .Add(p => p.Descriptor, descriptor)
                .Add(p => p.Context, NewContext($"v{iteration}")));
        }

        sink.RecentEvents.Count.ShouldBe(ExpectedDiagnosticCount);
    }

    [Fact]
    public void Render_TwoSiblingHosts_OneFails_OtherKeepsRendering() {
        // P18 / AC8 — sibling-survival. A failing host must not take down a sibling host
        // rendered alongside it. We compose two hosts inside a wrapper so bUnit can render
        // them as siblings of a single tree.
        IRenderedComponent<TwoTemplateHostsHarness> cut = Render<TwoTemplateHostsHarness>(parameters => parameters
            .Add(p => p.HealthyContext, NewContext("HealthyValue"))
            .Add(p => p.FaultyContext, NewContext("FaultyValue"))
            .Add(p => p.HealthyDescriptor, NewDescriptor(typeof(EchoTemplate)))
            .Add(p => p.FaultyDescriptor, NewDescriptor(typeof(ThrowingTemplate))));

        cut.Markup.ShouldContain("template-greeting=\"HealthyValue\"");
        AssertDiagnosticVisibility(cut.Markup, "role=\"alert\"");
        AssertDiagnosticVisibility(cut.Markup, FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault);
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
        IRenderedComponent<FcProjectionTemplateHost<TemplateProjection>> cut = Render<FcProjectionTemplateHost<TemplateProjection>>(parameters => parameters
            .Add(p => p.Descriptor, NewDescriptor(typeof(ThrowingTemplate)))
            .Add(p => p.Context, NewContext("SensitivePayloadSentinel")));
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task Render_ThrowingGeneratedFallback_IsContainedWithoutRecursiveRecovery(bool childThrows)
    {
        IErrorBoundaryLogger boundaryLogger = Substitute.For<IErrorBoundaryLogger>();
        Services.AddSingleton(boundaryLogger);
        InMemoryDiagnosticSink sink = new(capacity: 8);
        Services.AddSingleton<IDiagnosticSink>(sink);
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
        ProjectionTemplateContext<TemplateProjection> context = NewContext("SensitiveRow", fallback);
        ProjectionTemplateDescriptor descriptor = NewDescriptor(typeof(ThrowingTemplate));
        IRenderedComponent<FcProjectionTemplateHost<TemplateProjection>> cut = Render<FcProjectionTemplateHost<TemplateProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor).Add(p => p.Context, context));
        cut.Render(parameters => parameters.Add(p => p.Descriptor, descriptor).Add(p => p.Context, context));
        attempts.ShouldBe(1);
        cut.Markup.ShouldContain("Data could not be loaded.");
        cut.Markup.ShouldNotContain("SensitiveFallbackPayload");
        cut.Markup.ShouldNotContain("SensitiveRow");
        _logger.Entries.ShouldHaveSingleItem().Message.ShouldNotContain("SensitiveFallbackPayload");
        await boundaryLogger.Received(1).LogErrorAsync(Arg.Any<Exception>()).ConfigureAwait(true);
        sink.RecentEvents.Count.ShouldBe(ExpectedDiagnosticCount);
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

    private static ProjectionTemplateDescriptor NewDescriptor(Type templateType)
        => new(typeof(TemplateProjection), null, templateType, ProjectionTemplateContractVersion.Current);

    private static ProjectionTemplateContext<TemplateProjection> NewContext(string greeting, RenderFragment? defaultBody = null)
        => new(
            projectionType: typeof(TemplateProjection),
            boundedContext: "Tests",
            role: null,
            renderContext: new RenderContext(TenantId: "tenant", UserId: "user", Mode: FcRenderMode.Server, DensityLevel: DensityLevel.Comfortable, IsReadOnly: false),
            items: [new TemplateProjection(1, greeting)],
            columns: [new ProjectionTemplateColumnDescriptor(greeting, greeting, null, null)],
            sections: [new ProjectionTemplateSectionDescriptor("Body", "Body", "Body")],
            defaultBody: defaultBody ?? (static builder => builder.AddContent(0, "Generated fallback")),
            sectionRenderer: static _ => static _ => { },
            rowRenderer: static _ => static _ => { },
            fieldRenderer: static (_, _) => static _ => { });

    public sealed record TemplateProjection(int Id, string Greeting);

    public sealed class EchoTemplate : ComponentBase {
        [Parameter]
        public ProjectionTemplateContext<TemplateProjection> Context { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            ArgumentNullException.ThrowIfNull(builder);
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "template-greeting", Context.Items.Count > 0 ? Context.Items[0].Greeting : string.Empty);
            builder.CloseElement();
        }
    }

    public sealed class ThrowingTemplate : ComponentBase {
        [Parameter]
        public ProjectionTemplateContext<TemplateProjection> Context { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => throw new InvalidOperationException("raw template exception");
    }

    /// <summary>Test harness that hosts two <see cref="FcProjectionTemplateHost{TProjection}"/> as siblings.</summary>
    public sealed class TwoTemplateHostsHarness : ComponentBase {
        [Parameter, EditorRequired]
        public ProjectionTemplateContext<TemplateProjection> HealthyContext { get; set; } = default!;

        [Parameter, EditorRequired]
        public ProjectionTemplateContext<TemplateProjection> FaultyContext { get; set; } = default!;

        [Parameter, EditorRequired]
        public ProjectionTemplateDescriptor HealthyDescriptor { get; set; } = default!;

        [Parameter, EditorRequired]
        public ProjectionTemplateDescriptor FaultyDescriptor { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            ArgumentNullException.ThrowIfNull(builder);
            builder.OpenComponent<FcProjectionTemplateHost<TemplateProjection>>(0);
            builder.AddAttribute(1, nameof(FcProjectionTemplateHost<TemplateProjection>.Descriptor), HealthyDescriptor);
            builder.AddAttribute(2, nameof(FcProjectionTemplateHost<TemplateProjection>.Context), HealthyContext);
            builder.CloseComponent();

            builder.OpenComponent<FcProjectionTemplateHost<TemplateProjection>>(3);
            builder.AddAttribute(4, nameof(FcProjectionTemplateHost<TemplateProjection>.Descriptor), FaultyDescriptor);
            builder.AddAttribute(5, nameof(FcProjectionTemplateHost<TemplateProjection>.Context), FaultyContext);
            builder.CloseComponent();
        }
    }

    private sealed class ListLogger<T> : ILogger<T> {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
