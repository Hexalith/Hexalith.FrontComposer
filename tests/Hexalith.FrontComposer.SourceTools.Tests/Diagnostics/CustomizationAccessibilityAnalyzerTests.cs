using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

public sealed class CustomizationAccessibilityAnalyzerTests {
    [Theory]
    [InlineData("HFC1050", """
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => { }));
        builder.CloseElement();
        """)]
    [InlineData("HFC1051", """
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "tabindex", -1);
        builder.CloseElement();
        """)]
    [InlineData("HFC1052", """
        builder.AddMarkupContent(0, "<style>.custom:focus { outline: none; }</style>");
        """)]
    [InlineData("HFC1053", """
        builder.OpenElement(0, "section");
        builder.AddAttribute(1, "data-fc-status", "loading");
        builder.CloseElement();
        """)]
    [InlineData("HFC1054", """
        builder.AddMarkupContent(0, "<style>.custom { transition: opacity .2s; }</style>");
        """)]
    [InlineData("HFC1055", """
        builder.AddMarkupContent(0, "<style>.custom { color: red; }</style>");
        """)]
    public async Task ProjectionTemplate_WithAccessibilityViolation_ReportsExpectedDiagnostic(string expectedId, string body) {
        Diagnostic[] diagnostics = await AnalyzeAsync(CreateProjectionTemplateSource(body));

        Diagnostic diagnostic = diagnostics.Single(d => d.Id == expectedId);
        diagnostic.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().ShouldContain("What:");
        diagnostic.GetMessage().ShouldContain("Expected:");
        diagnostic.GetMessage().ShouldContain("Got:");
        diagnostic.GetMessage().ShouldContain("Fix:");
        diagnostic.GetMessage().ShouldContain("Fallback:");
        diagnostic.GetMessage().ShouldContain($"DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/{expectedId}");
    }

    [Fact]
    public async Task ProjectionTemplate_WithAccessibleName_DoesNotReportAccessibleNameWarning() {
        Diagnostic[] diagnostics = await AnalyzeAsync(CreateProjectionTemplateSource("""
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "aria-label", "Refresh counter");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => { }));
            builder.CloseElement();
            """));

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

    [Fact]
    public async Task ProjectionTemplate_WithCommentedOutViolations_DoesNotReportAccessibilityWarnings() {
        Diagnostic[] diagnostics = await AnalyzeAsync(CreateProjectionTemplateSource("""
            // builder.AddAttribute(0, "onclick", EventCallback.Factory.Create(this, () => { }));
            // builder.AddAttribute(1, "tabindex", -1);
            /* builder.AddMarkupContent(2, "<style>.custom:focus { outline: none; }</style>"); */
            /* builder.AddAttribute(3, "data-fc-status", "loading"); */
            /* builder.AddMarkupContent(4, "<style>.custom { transition: opacity .2s; color: red; }</style>"); */
            builder.OpenElement(5, "div");
            builder.AddContent(6, "Safe custom content");
            builder.CloseElement();
            """));

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectionTemplate_WithReducedMotionAndForcedColorsFallbackEvidence_DoesNotReportFallbackWarnings() {
        Diagnostic[] diagnostics = await AnalyzeAsync(CreateProjectionTemplateSource("""
            builder.AddMarkupContent(0, "<style>.custom { transition: opacity .2s; color: red; } @media (prefers-reduced-motion: reduce) { .custom { transition: none; } } @media (forced-colors: active) { .custom { color: CanvasText; } }</style>");
            """));

        diagnostics.Select(static d => d.Id).ShouldNotContain("HFC1054");
        diagnostics.Select(static d => d.Id).ShouldNotContain("HFC1055");
    }

    [Fact]
    public async Task SlotOverrideRegistration_DiscoveredComponent_ReportsAccessibilityWarning() {
        Diagnostic[] diagnostics = await AnalyzeAsync(CreateRegistrationSource(
            registration: "services.AddSlotOverride<CounterProjection, int, SlotOverrideComponent>(p => p.Count);",
            componentName: "SlotOverrideComponent",
            body: """
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, () => { }));
                builder.CloseElement();
                """));

        diagnostics.Select(static d => d.Id).ShouldContain("HFC1050");
    }

    [Fact]
    public async Task ViewOverrideRegistration_DiscoveredComponent_ReportsAccessibilityWarning() {
        Diagnostic[] diagnostics = await AnalyzeAsync(CreateRegistrationSource(
            registration: "services.AddViewOverride<CounterProjection, ViewOverrideComponent>();",
            componentName: "ViewOverrideComponent",
            body: """
                builder.AddMarkupContent(0, "<style>.custom { color: red; }</style>");
                """));

        diagnostics.Select(static d => d.Id).ShouldContain("HFC1055");
    }

    private static async Task<Diagnostic[]> AnalyzeAsync(string source) {
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        DiagnosticAnalyzer analyzer = new CustomizationAccessibilityAnalyzer();
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers([analyzer]);
        return [.. await withAnalyzers.GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false)];
    }

    private static string CreateProjectionTemplateSource(string body)
        => $$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace Demo;

            [Projection]
            public sealed partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
            public sealed class TestTemplate : ComponentBase {
                [Parameter] public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;

                protected override void BuildRenderTree(RenderTreeBuilder builder) {
            {{body}}
                }
            }
            """;

    private static string CreateRegistrationSource(string registration, string componentName, string body)
        => $$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Hexalith.FrontComposer.Shell.Extensions;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            using Microsoft.Extensions.DependencyInjection;

            namespace Demo;

            [Projection]
            public sealed partial class CounterProjection { public int Count { get; set; } }

            public static class Registration {
                public static void Configure(IServiceCollection services) {
                    {{registration}}
                }
            }

            public sealed class {{componentName}} : ComponentBase {
                protected override void BuildRenderTree(RenderTreeBuilder builder) {
            {{body}}
                }
            }
            """;
}
