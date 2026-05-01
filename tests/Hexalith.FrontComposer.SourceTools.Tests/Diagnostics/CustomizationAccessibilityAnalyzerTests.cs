using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

public sealed class CustomizationAccessibilityAnalyzerTests {
    [Fact]
    public async Task ProjectionTemplate_WithClickableUnnamedRoot_ReportsAccessibleNameWarning() {
        Diagnostic[] diagnostics = await AnalyzeAsync("""
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace Demo;

            [Projection]
            public sealed partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
            public sealed class BadTemplate : ComponentBase {
                [Parameter] public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;

                protected override void BuildRenderTree(RenderTreeBuilder builder) {
                    builder.OpenElement(0, "button");
                    builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => { }));
                    builder.CloseElement();
                }
            }
            """);

        diagnostics.Select(static d => d.Id).ShouldContain("HFC1050");
        diagnostics.Single(static d => d.Id == "HFC1050").GetMessage().ShouldContain("DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1050");
    }

    [Fact]
    public async Task ProjectionTemplate_WithAccessibleName_DoesNotReportAccessibleNameWarning() {
        Diagnostic[] diagnostics = await AnalyzeAsync("""
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace Demo;

            [Projection]
            public sealed partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
            public sealed class GoodTemplate : ComponentBase {
                [Parameter] public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;

                protected override void BuildRenderTree(RenderTreeBuilder builder) {
                    builder.OpenElement(0, "button");
                    builder.AddAttribute(1, "aria-label", "Refresh counter");
                    builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => { }));
                    builder.CloseElement();
                }
            }
            """);

        diagnostics.Select(static d => d.Id).ShouldNotContain("HFC1050");
    }

    [Fact]
    public async Task NonCustomizationComponent_IsNotAnalyzed() {
        Diagnostic[] diagnostics = await AnalyzeAsync("""
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace Demo;

            public sealed class OrdinaryComponent : ComponentBase {
                protected override void BuildRenderTree(RenderTreeBuilder builder) {
                    builder.OpenElement(0, "button");
                    builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => { }));
                    builder.CloseElement();
                }
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    private static async Task<Diagnostic[]> AnalyzeAsync(string source) {
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        DiagnosticAnalyzer analyzer = new CustomizationAccessibilityAnalyzer();
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers([analyzer]);
        return [.. await withAnalyzers.GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false)];
    }
}
