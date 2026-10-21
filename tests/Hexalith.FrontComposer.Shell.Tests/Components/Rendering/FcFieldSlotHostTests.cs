using Bunit;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>
/// Story 6-3 GB-P2 — bUnit coverage of <see cref="FcFieldSlotHost{TProjection,TField}"/>:
/// descriptor-resolved render path, RenderDefault fallback path, null-parameter early-return
/// + diagnostic, and FieldType vs typeof(TField) re-validation (GB-P14).
/// </summary>
public sealed class FcFieldSlotHostTests : BunitContext {
    private readonly ListLogger<FcFieldSlotHost<SlotProjection, int>> _logger = new();

    public FcFieldSlotHostTests() {
        _ = Services.AddLogging();
        // P1 / P2 — diagnostic panel injects IStringLocalizer<FcShellResources> and uses
        // Fluent UI primitives. Register localization + Fluent UI services so the panel
        // can render in the test bUnit context.
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        Services.AddSingleton(environment);
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddFluentUIComponents();
        _ = Services.AddLocalization();
        Services.Replace(ServiceDescriptor.Singleton<ILogger<FcFieldSlotHost<SlotProjection, int>>>(_logger));
    }

    [Fact]
    public void Render_WithRegisteredSlot_RendersSlotComponentAndPassesContext() {
        ProjectionSlotRegistry registry = new(
            NullLogger<ProjectionSlotRegistry>.Instance,
            [new ProjectionSlotDescriptorSource([
                new ProjectionSlotDescriptor(
                    ProjectionType: typeof(SlotProjection),
                    FieldName: "Priority",
                    FieldType: typeof(int),
                    Role: null,
                    ComponentType: typeof(EchoPrioritySlot),
                    ContractVersion: ProjectionSlotContractVersion.Current),
            ])]);
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(registry));

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(7, "Bob"))
            .Add(p => p.Value, 7)
            .Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext()));

        cut.Markup.ShouldContain("priority=7");
    }

    [Fact]
    public void Render_NoDescriptor_FallsBackToRenderDefault() {
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(EmptyRegistry()));

        RenderFragment defaultFragment(FieldSlotContext<SlotProjection, int> ctx) => builder => {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-default", "true");
            builder.AddContent(2, $"v={ctx.Value}");
            builder.CloseElement();
        };

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(3, "Sue"))
            .Add(p => p.Value, 3)
            .Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext())
            .Add(p => p.RenderDefault, defaultFragment));

        cut.Markup.ShouldContain("data-default=\"true\"");
        cut.Markup.ShouldContain("v=3");
    }

    [Fact]
    public void Render_NullParent_LogsHfc2120_And_RendersNothing() {
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(EmptyRegistry()));

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, null!)
            .Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext()));

        cut.Markup.Trim().ShouldBeEmpty();
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning
            && e.EventId.Id == 5806
            && e.EventId.Name == "FieldSlotMissingParameters"
            && e.Message.Contains("HFC2120")
            && e.Message.Contains("ParentNull=True"));
    }

    [Fact]
    public void Render_NullField_LogsHfc2120_And_RendersNothing() {
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(EmptyRegistry()));

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(1, "x"))
            .Add(p => p.Field, null!)
            .Add(p => p.RenderContext, NewRenderContext()));

        cut.Markup.Trim().ShouldBeEmpty();
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning
            && e.EventId.Id == 5806
            && e.EventId.Name == "FieldSlotMissingParameters"
            && e.Message.Contains("HFC2120")
            && e.Message.Contains("FieldNull=True"));
    }

    [Fact]
    public void Render_DescriptorFieldTypeMismatch_LogsHfc1039_And_FallsBackToRenderDefault() {
        ProjectionSlotRegistry registry = new(
            NullLogger<ProjectionSlotRegistry>.Instance,
            [new ProjectionSlotDescriptorSource([
                // Descriptor declares FieldType=string, but the host's TField is int.
                // Hand-built to simulate generator drift; AddSlotOverride users cannot reach this.
                new ProjectionSlotDescriptor(
                    ProjectionType: typeof(SlotProjection),
                    FieldName: "Priority",
                    FieldType: typeof(string),
                    Role: null,
                    ComponentType: typeof(StringContextSlot),
                    ContractVersion: ProjectionSlotContractVersion.Current),
            ])]);
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(registry));

        bool defaultInvoked = false;
        RenderFragment defaultFragment(FieldSlotContext<SlotProjection, int> _) => builder => {
            defaultInvoked = true;
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-fallback", "true");
            builder.CloseElement();
        };

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(2, "y"))
            .Add(p => p.Value, 2)
            .Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext())
            .Add(p => p.RenderDefault, defaultFragment));

        defaultInvoked.ShouldBeTrue();
        cut.Markup.ShouldContain("data-fallback=\"true\"");
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("HFC1039"));
    }

    [Fact]
    public void Render_ThrowingSlot_IsolatesFault_DoesNotLeakItemPayloadOrException() {
        // P7 — Level 3 redaction adversarial test (mirrors Level 4 pattern). The slot
        // component throws with sensitive-looking text and the parent projection carries a
        // value that must not appear in the diagnostic panel or log output.
        ProjectionSlotRegistry registry = new(
            NullLogger<ProjectionSlotRegistry>.Instance,
            [new ProjectionSlotDescriptorSource([
                new ProjectionSlotDescriptor(
                    ProjectionType: typeof(SlotProjection),
                    FieldName: "Priority",
                    FieldType: typeof(int),
                    Role: null,
                    ComponentType: typeof(ThrowingPrioritySlot),
                    ContractVersion: ProjectionSlotContractVersion.Current),
            ])]);
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(registry));
        InMemoryDiagnosticSink sink = new(capacity: 4);
        Services.AddSingleton<IDiagnosticSink>(sink);

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(42, "PayloadValueMustNotLog"))
            .Add(p => p.Value, 42)
            .Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext())
            .Add(p => p.RenderDefault, _ => builder => builder.AddContent(0, "Generated field fallback")));

        cut.Markup.ShouldContain("Generated field fallback");
        // Diagnostic panel renders inside the bounded host.
        AssertDiagnosticVisibility(cut.Markup, FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault);
        AssertDiagnosticVisibility(cut.Markup, "role=\"alert\"");

        // Log entry redaction.
        var (_, _, Message) = _logger.Entries
            .Where(e => e.Message.Contains(FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault))
            .ShouldHaveSingleItem();
        Message.ShouldContain("HFC2115");
        Message.ShouldNotContain("PayloadValueMustNotLog");
        Message.ShouldNotContain("raw slot exception");

        // Sink event redaction.
#if DEBUG
        DevDiagnosticEvent evt = sink.RecentEvents.ShouldHaveSingleItem();
        evt.Code.ShouldBe(FcDiagnosticIds.HFC2115_CustomizationOverrideRenderFault);
        evt.Message.ShouldContain("What:");
        evt.Message.ShouldContain("Expected:");
        evt.Message.ShouldContain("Got:");
        evt.Message.ShouldContain("Fix:");
        evt.Message.ShouldContain("DocsLink:");
        evt.Message.ShouldNotContain("PayloadValueMustNotLog");
        evt.Message.ShouldNotContain("raw slot exception");
#else
        sink.RecentEvents.ShouldBeEmpty();
#endif
    }

    [Fact]
    public void Render_ThrowingSlot_PublishesDiagnosticOnce_OnRepeatedRenders() {
        // P18 — publish-once / no-dup-on-rerender. Repeated parent re-renders within a
        // single fault episode must not flood IDiagnosticSink with duplicate entries.
        ProjectionSlotRegistry registry = new(
            NullLogger<ProjectionSlotRegistry>.Instance,
            [new ProjectionSlotDescriptorSource([
                new ProjectionSlotDescriptor(
                    ProjectionType: typeof(SlotProjection),
                    FieldName: "Priority",
                    FieldType: typeof(int),
                    Role: null,
                    ComponentType: typeof(ThrowingPrioritySlot),
                    ContractVersion: ProjectionSlotContractVersion.Current),
            ])]);
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(registry));
        InMemoryDiagnosticSink sink = new(capacity: 8);
        Services.AddSingleton<IDiagnosticSink>(sink);

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(1, "v1"))
            .Add(p => p.Value, 1)
            .Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext()));

        sink.RecentEvents.Count.ShouldBe(ExpectedDiagnosticCount);

        // Repeated re-renders with the same descriptor / context: no fresh diagnostics.
        for (int i = 2; i <= 5; i++) {
            int iteration = i;
            cut.Render(parameters => parameters
                .Add(p => p.Parent, new SlotProjection(iteration, $"v{iteration}"))
                .Add(p => p.Value, iteration)
                .Add(p => p.Field, NewField("Priority"))
                .Add(p => p.RenderContext, NewRenderContext()));
        }

        sink.RecentEvents.Count.ShouldBe(ExpectedDiagnosticCount);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData(null)]
    public void Render_FieldFault_RetainsGeneratedFieldInEveryEnvironment(string? environmentName)
    {
        Services.RemoveAll<IHostEnvironment>();
        if (environmentName is not null)
        {
            IHostEnvironment environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            Services.AddSingleton(environment);
        }
        ProjectionSlotRegistry registry = new(NullLogger<ProjectionSlotRegistry>.Instance,
            [new ProjectionSlotDescriptorSource([new ProjectionSlotDescriptor(typeof(SlotProjection), "Priority", typeof(int), null,
                typeof(ThrowingPrioritySlot), ProjectionSlotContractVersion.Current)])]);
        Services.AddSingleton<IProjectionSlotRegistry>(registry);
        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(42, "SensitivePayloadSentinel"))
            .Add(p => p.Value, 42).Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext())
            .Add(p => p.RenderDefault, context => builder => builder.AddContent(0, "Generated field " + context.Value)));
        cut.Markup.ShouldContain("Generated field 42");
        cut.Markup.ShouldNotContain("SensitivePayloadSentinel");
        cut.Markup.ShouldNotContain("test-tenant");
        cut.Markup.ShouldNotContain("test-user");
        cut.Markup.ShouldNotContain("raw slot exception");
        bool visible = false;
#if DEBUG
        visible = environmentName == "Development";
#endif
        cut.FindAll("[data-fc-diagnostic]").Count.ShouldBe(visible ? 1 : 0);
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
        ProjectionSlotRegistry registry = new(NullLogger<ProjectionSlotRegistry>.Instance,
            [new ProjectionSlotDescriptorSource([new ProjectionSlotDescriptor(typeof(SlotProjection), "Priority", typeof(int), null,
                typeof(ThrowingPrioritySlot), ProjectionSlotContractVersion.Current)])]);
        Services.AddSingleton<IProjectionSlotRegistry>(registry);
        int attempts = 0;
        RenderFragment<FieldSlotContext<SlotProjection, int>> fallback = _ =>
        {
            attempts++;
            if (!childThrows)
            {
                throw new InvalidOperationException("SensitiveFallbackPayload token stack trace");
            }
            return builder =>
            {
                builder.OpenComponent<ThrowingGeneratedFallback>(0);
                builder.CloseComponent();
            };
        };
        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(42, "SensitiveRow"))
            .Add(p => p.Value, 42).Add(p => p.Field, NewField("Priority"))
            .Add(p => p.RenderContext, NewRenderContext()).Add(p => p.RenderDefault, fallback));
        cut.Render();
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

    private static FieldDescriptor NewField(string name)
        => new(Name: name, TypeName: "System.Int32", IsNullable: false, DisplayName: name, Order: 0);

    private static RenderContext NewRenderContext()
        => new(TenantId: "test-tenant", UserId: "test-user", Mode: FcRenderMode.Server, DensityLevel: DensityLevel.Comfortable, IsReadOnly: false);

    private static ProjectionSlotRegistry EmptyRegistry()
        => new(NullLogger<ProjectionSlotRegistry>.Instance, []);

    public sealed record SlotProjection(int Priority, string Name);

    public sealed class EchoPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
            ArgumentNullException.ThrowIfNull(builder);
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-priority", Context.Value);
            builder.AddContent(2, $"priority={Context.Value}");
            builder.CloseElement();
        }
    }

    public sealed class StringContextSlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, string> Context { get; set; } = default!;
    }

    public sealed class ThrowingPrioritySlot : ComponentBase {
        [Parameter]
        public FieldSlotContext<SlotProjection, int> Context { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            => throw new InvalidOperationException("raw slot exception");
    }

    private sealed class ListLogger<T> : ILogger<T> {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
