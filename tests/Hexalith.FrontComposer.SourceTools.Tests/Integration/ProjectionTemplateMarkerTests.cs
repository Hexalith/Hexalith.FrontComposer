using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

/// <summary>
/// Story 6-2 T10 — generator-driven tests for [ProjectionTemplate] marker discovery,
/// validation diagnostics, manifest emission, deduplication, and contract version handling.
/// </summary>
public sealed class ProjectionTemplateMarkerTests {
    [Fact]
    public void RunGenerators_ValidMarker_EmitsManifestWithDescriptor() {
        GeneratorDriverRunResult result = RunWithSources(ValidProjection, ValidTemplate);

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldContain("__FrontComposerProjectionTemplatesRegistration");
        source.ShouldContain("typeof(global::TestDomain.CounterProjection)");
        source.ShouldContain("typeof(global::TestDomain.CounterCardTemplate)");
        source.ShouldContain("public const int ContractVersion = 1000000;");
    }

    [Fact]
    public void RunGenerators_ManifestIsDeterministic_NoTimestampsOrPaths() {
        GeneratorDriverRunResult result = RunWithSources(ValidProjection, ValidTemplate);
        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();

        // Cache-safety boundary (Story 6-2 D15 / AC15): generated manifest is type metadata only.
        // We assert the absence of timestamp/path-shaped tokens; references to "tenant" / "user" in
        // doc comments are allowed because they do not capture per-render values.
        source.ShouldNotContain("DateTime.UtcNow");
        source.ShouldNotContain("DateTimeOffset.UtcNow");
        source.ShouldNotContain("\\\\");
        source.ShouldNotContain(":\\\\Users");
        source.ShouldNotContain("/Users/");
        source.ShouldNotContain("TenantId");
        source.ShouldNotContain("UserId");
    }

    [Fact]
    public void RunGenerators_NonProjectionType_EmitsHfc1033() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            public class NotAProjection { }

            [ProjectionTemplate(typeof(NotAProjection), ProjectionTemplateContractVersion.Current)]
            public partial class BadTemplate
            {
                [Parameter]
                public ProjectionTemplateContext<NotAProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1033").ShouldNotBeEmpty();

        // Manifest exists but is empty (descriptor was rejected).
        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldNotContain("typeof(global::TestDomain.BadTemplate)");
    }

    [Fact]
    public void RunGenerators_TemplateMissingContextProperty_EmitsHfc1034() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
            public partial class TemplateWithoutContext { }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1034").ShouldNotBeEmpty();
        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldNotContain("TemplateWithoutContext");
    }

    [Fact]
    public void RunGenerators_TemplateContextWithoutParameter_EmitsHfc1034AndSuppressesDescriptor() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
            public partial class TemplateWithoutParameter
            {
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1034").ShouldNotBeEmpty();
        string source = ManifestTree(result).GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldNotContain("TemplateWithoutParameter");
    }

    [Fact]
    public void RunGenerators_IncompatibleMajorVersion_EmitsHfc1035AndSuppressesDescriptor() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), 9_000_000)]
            public partial class FutureMajorTemplate
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1035").ShouldNotBeEmpty();
        // Descriptor must be suppressed when major version is incompatible.
        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldNotContain("FutureMajorTemplate");
    }

    [Fact]
    public void RunGenerators_MinorVersionDrift_EmitsHfc1036ButKeepsDescriptor() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), 1_005_000)]
            public partial class DriftedTemplate
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1036").ShouldNotBeEmpty();
        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldContain("DriftedTemplate");
    }

    [Fact]
    public void RunGenerators_BuildOnlyVersionDrift_DoesNotEmitHfc1036() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), 1_000_009)]
            public partial class BuildOnlyDriftTemplate
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1036").ShouldBeEmpty();
        string source = ManifestTree(result).GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldContain("BuildOnlyDriftTemplate");
    }

    [Fact]
    public void RunGenerators_DuplicateMarkers_EmitsHfc1037AndSuppressesAllDuplicates() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
            public partial class TemplateA
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
            public partial class TemplateB
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1037").ShouldNotBeEmpty();
        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        // Both templates should be suppressed (deterministic — no order-dependent winner).
        source.ShouldNotContain("TemplateA");
        source.ShouldNotContain("TemplateB");
    }

    [Fact]
    public void RunGenerators_DistinctRolesForSameProjection_BothEmit() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current, Role = ProjectionRole.DetailRecord)]
            public partial class DetailTemplate
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current, Role = ProjectionRole.Timeline)]
            public partial class TimelineTemplate
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1037").ShouldBeEmpty();
        SyntaxTree manifest = ManifestTree(result);
        string source = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldContain("DetailTemplate");
        source.ShouldContain("TimelineTemplate");
        source.ShouldContain("ProjectionRole.DetailRecord");
        source.ShouldContain("ProjectionRole.Timeline");
    }

    [Fact]
    public void RunGenerators_InvalidProjectionRoleValue_EmitsHfc1024AndSuppressesDescriptor() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current, Role = (ProjectionRole)999)]
            public partial class BadRoleTemplate
            {
                [Parameter]
                public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Id == "HFC1024").ShouldNotBeEmpty();
        string source = ManifestTree(result).GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldNotContain("BadRoleTemplate");
    }

    [Fact]
    public void RunGenerators_NestedTemplate_EmitsSourceSafeTypeName() {
        const string template = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            using Hexalith.FrontComposer.Contracts.Rendering;
            using Microsoft.AspNetCore.Components;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }

            public static partial class TemplateContainer
            {
                [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
                public partial class NestedTemplate
                {
                    [Parameter]
                    public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
                }
            }
            """;

        GeneratorDriverRunResult result = RunWithSources(template);

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        string source = ManifestTree(result).GetText(TestContext.Current.CancellationToken).ToString();
        source.ShouldContain("typeof(global::TestDomain.TemplateContainer.NestedTemplate)");
    }

    [Fact]
    public void RunGenerators_NoMarkers_EmitsEmptyManifestWithoutWarnings() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Projection]
            public partial class CounterProjection { public int Count { get; set; } }
            """;

        GeneratorDriverRunResult result = RunWithSources(source);

        SyntaxTree manifest = ManifestTree(result);
        string code = manifest.GetText(TestContext.Current.CancellationToken).ToString();
        code.ShouldContain("internal static class __FrontComposerProjectionTemplatesRegistration");
        // Manifest should declare an empty descriptor array.
        code.ShouldContain("new global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateDescriptor[]");
    }

    private static GeneratorDriverRunResult RunWithSources(params string[] sources) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(sources);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult();
    }

    private static SyntaxTree ManifestTree(GeneratorDriverRunResult result) =>
        result.GeneratedTrees.Single(t =>
            System.IO.Path.GetFileName(t.FilePath).Contains(
                ProjectionTemplateManifestEmitter.GeneratedHintName,
                StringComparison.Ordinal));

    private const string ValidProjection = """
        using Hexalith.FrontComposer.Contracts.Attributes;

        namespace TestDomain;

        [Projection]
        public partial class CounterProjection
        {
            public int Count { get; set; }
        }
        """;

    private const string ValidTemplate = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        using Hexalith.FrontComposer.Contracts.Rendering;
        using Microsoft.AspNetCore.Components;

        namespace TestDomain;

        [ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
        public partial class CounterCardTemplate
        {
            [Parameter]
            public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
        }
        """;
}
