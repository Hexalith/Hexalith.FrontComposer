using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

public sealed class ProjectionViewOverrideRegistrationAnalyzerTests
{
    [Theory]
    [InlineData("[Parameter] public ProjectionViewContext<Other> Context { get; set; } = null!;")]
    [InlineData("public ProjectionViewContext<Projection> Context { get; set; } = null!;")]
    [InlineData("[Parameter] public ProjectionViewContext<Projection> Context { get; private set; } = null!;")]
    [InlineData("[Parameter] public static ProjectionViewContext<Projection> Context { get; set; } = null!;")]
    [InlineData("[Parameter(CaptureUnmatchedValues = true)] public ProjectionViewContext<Projection> Context { get; set; } = null!;")]
    [InlineData("[Parameter, CascadingParameter] public ProjectionViewContext<Projection> Context { get; set; } = null!;")]
    [InlineData("[Parameter, SupplyParameterFromForm] public ProjectionViewContext<Projection> Context { get; set; } = null!;")]
    [InlineData("")]
    public async Task InvalidTypedRegistrationReportsHfc1043(string property)
    {
        Diagnostic[] diagnostics = await AnalyzeAsync("services.AddViewOverride<Projection, Replacement>();", property);

        Diagnostic diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe("HFC1043");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan)
            .ShouldBe("services.AddViewOverride<Projection, Replacement>()");
        diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)
            .ShouldContain("ProjectionViewContext<Demo.Projection>");
    }

    [Fact]
    public async Task RuntimeEquivalentTypeArgumentsAreAccepted()
    {
        Diagnostic[] diagnostics = await AnalyzeAsync(
            "services.AddViewOverride<Box<(object Left, int Right)>, Replacement>();",
            "[Parameter] public ProjectionViewContext<Box<(dynamic Value, int Count)>> Context { get; set; } = null!;");

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task CompatibleInheritedContextIsAccepted()
    {
        Diagnostic[] diagnostics = await AnalyzeAsync(
            "ProjectionViewOverrideServiceCollectionExtensions.AddViewOverride<Projection, InheritedReplacement>(services);",
            "[Parameter] public ProjectionViewContext<Projection> Context { get; set; } = null!;");

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task GenericRegistrationDefersToStartupValidation()
    {
        Diagnostic[] diagnostics = await AnalyzeAsync(
            "services.AddViewOverride<T, Replacement>();",
            "[Parameter] public ProjectionViewContext<Projection> Context { get; set; } = null!;");

        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("services.AddViewOverride<T, Replacement>();")]
    [InlineData("services.AddViewOverride<Projection, GenericReplacement<T>>();")]
    [InlineData("System.Func<IServiceCollection, ProjectionRole?, string, IServiceCollection> register = ProjectionViewOverrideServiceCollectionExtensions.AddViewOverride<Projection, Replacement>;")]
    public async Task StaticallyProvenMissingContextIsReported(string registration)
    {
        Diagnostic[] diagnostics = await AnalyzeAsync(registration, "");

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe("HFC1043");
    }

    [Fact]
    public async Task DifferentMethodWithSameNameIsIgnored()
    {
        Diagnostic[] diagnostics = await AnalyzeAsync("OtherRegistry.AddViewOverride<Projection, Replacement>();", "");

        diagnostics.ShouldBeEmpty();
    }

    private static async Task<Diagnostic[]> AnalyzeAsync(string registration, string property)
    {
        Compilation compilation = CompilationHelper.CreateCompilation($$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Hexalith.FrontComposer.Shell.Extensions;
            using Microsoft.AspNetCore.Components;
            using Microsoft.Extensions.DependencyInjection;

            namespace Demo;

            public sealed class Projection { }
            public sealed class Other { }
            public sealed class Box<T> { }
            public class Replacement : ComponentBase { {{property}} }
            public sealed class GenericReplacement<T> : Replacement { }
            public sealed class InheritedReplacement : Replacement { }

            public static class OtherRegistry {
                public static void AddViewOverride<TProjection, TComponent>() { }
                public static void Register<T>(IServiceCollection services) {
                    {{registration}}
                }
            }
            """);
        CancellationToken token = TestContext.Current.CancellationToken;
        compilation.GetDiagnostics(token).Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        DiagnosticAnalyzer analyzer = new ProjectionViewOverrideRegistrationAnalyzer();
        return [.. await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync(token).ConfigureAwait(false)];
    }
}
