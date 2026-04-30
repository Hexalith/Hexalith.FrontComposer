using Bunit;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Services.ProjectionSlots;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

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

        RenderFragment<FieldSlotContext<SlotProjection, int>> defaultFragment = ctx => builder => {
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
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("HFC2120") && e.Message.Contains("ParentNull: True"));
    }

    [Fact]
    public void Render_NullField_LogsHfc2120_And_RendersNothing() {
        Services.Replace(ServiceDescriptor.Singleton<IProjectionSlotRegistry>(EmptyRegistry()));

        IRenderedComponent<FcFieldSlotHost<SlotProjection, int>> cut = Render<FcFieldSlotHost<SlotProjection, int>>(parameters => parameters
            .Add(p => p.Parent, new SlotProjection(1, "x"))
            .Add(p => p.Field, null!)
            .Add(p => p.RenderContext, NewRenderContext()));

        cut.Markup.Trim().ShouldBeEmpty();
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("HFC2120") && e.Message.Contains("FieldNull: True"));
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
        RenderFragment<FieldSlotContext<SlotProjection, int>> defaultFragment = _ => builder => {
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

    private sealed class ListLogger<T> : ILogger<T> {
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

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
