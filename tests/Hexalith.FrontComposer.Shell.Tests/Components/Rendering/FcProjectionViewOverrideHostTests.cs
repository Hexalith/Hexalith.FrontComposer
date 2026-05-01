using Bunit;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcProjectionViewOverrideHostTests : BunitContext {
    private readonly ListLogger<FcProjectionViewOverrideHost<ViewProjection>> _logger = new();

    public FcProjectionViewOverrideHostTests()
        => Services.Replace(ServiceDescriptor.Singleton<ILogger<FcProjectionViewOverrideHost<ViewProjection>>>(_logger));

    [Fact]
    public void Render_ValidReplacement_PassesFreshContext() {
        ProjectionViewContext<ViewProjection> context = NewContext([new ViewProjection(7, "Alpha")]);
        ProjectionViewOverrideDescriptor descriptor = NewDescriptor(typeof(EchoReplacement));

        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, context));

        cut.Markup.ShouldContain("override-count=\"1\"");
        cut.Markup.ShouldContain("Alpha");
    }

    [Fact]
    public void Render_ThrowingReplacement_IsolatesFault_AndRendersDiagnosticFallback() {
        ProjectionViewContext<ViewProjection> context = NewContext([new ViewProjection(7, "PayloadValueMustNotLog")]);
        ProjectionViewOverrideDescriptor descriptor = NewDescriptor(typeof(ThrowingReplacement));

        IRenderedComponent<FcProjectionViewOverrideHost<ViewProjection>> cut = Render<FcProjectionViewOverrideHost<ViewProjection>>(parameters => parameters
            .Add(p => p.Descriptor, descriptor)
            .Add(p => p.Context, context));

        cut.Markup.ShouldContain("role=\"alert\"");
        cut.Markup.ShouldContain(FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
        _logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains(FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault));
        _logger.Entries.ShouldNotContain(e => e.Message.Contains("PayloadValueMustNotLog"));
        _logger.Entries.ShouldNotContain(e => e.Message.Contains("raw replacement message"));
        _logger.Entries.ShouldAllBe(e => e.Exception == null);
    }

    private static ProjectionViewOverrideDescriptor NewDescriptor(Type componentType)
        => new(
            typeof(ViewProjection),
            null,
            componentType,
            ProjectionViewOverrideContractVersion.Current,
            "test");

    private static ProjectionViewContext<ViewProjection> NewContext(IReadOnlyList<ViewProjection> items)
        => new(
            projectionType: typeof(ViewProjection),
            boundedContext: "Tests",
            role: null,
            items: items,
            renderContext: new RenderContext("tenant", "user", FcRenderMode.Server, DensityLevel.Comfortable, IsReadOnly: false),
            columns: [new ProjectionTemplateColumnDescriptor("Name", "Name", null, null)],
            sections: [new ProjectionTemplateSectionDescriptor("Body", "Body", "Body")],
            lifecycleState: "Loaded",
            entityLabel: "Projection",
            entityPluralLabel: "Projections",
            defaultBody: static _ => { },
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
            builder.AddContent(2, Context.Items[0].Name);
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
