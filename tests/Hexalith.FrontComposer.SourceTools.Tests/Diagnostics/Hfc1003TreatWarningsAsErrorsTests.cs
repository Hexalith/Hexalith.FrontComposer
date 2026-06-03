using Hexalith.FrontComposer.SourceTools.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 2.1 AC3 — a non-<c>partial</c> <c>[Projection]</c> reports HFC1003 and, because
/// <c>TreatWarningsAsErrors=true</c> (TWAE) is on repo-wide, the HFC1003 <em>Warning</em> is promoted
/// to a build-breaking <em>error</em>. The parse pin
/// (<c>AttributeParserTests.Parse_NonPartialProjection_EmitsHFC1003</c>) proves the diagnostic is
/// produced; these pins prove the end-to-end behaviour the AC actually asserts — that HFC1003 is a
/// real, on-by-default Warning, that nothing downgrades it, and that under TWAE it surfaces as an
/// Error. Per the story we assert the descriptor + a deterministic generator-driver promotion rather
/// than running an actual <c>dotnet build</c> of a broken fixture.
/// </summary>
public class Hfc1003TreatWarningsAsErrorsTests {
    [Fact]
    public void Hfc1003Descriptor_IsOnByDefaultWarning() {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.ProjectionShouldBePartial;

        descriptor.Id.ShouldBe("HFC1003");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
        descriptor.Category.ShouldBe("HexalithFrontComposer");
    }

    [Fact]
    public void NonPartialProjection_ReportsHfc1003_AsWarning() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NonPartialProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);

        Diagnostic hfc1003 = driver.GetRunResult().Diagnostics.Single(d => d.Id == "HFC1003");
        hfc1003.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        hfc1003.Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void NonPartialProjection_Hfc1003_IsPromotedToError_UnderTreatWarningsAsErrors() {
        // TWAE is the compiler's general diagnostic option = Error. The generator driver applies the
        // compilation's diagnostic options to generator-reported diagnostics, so under TWAE the
        // HFC1003 Warning surfaces as an Error — i.e. the build fails rather than silently generating
        // against an unusable (non-partial) type.
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.NonPartialProjection);
        CSharpCompilation twae = compilation.WithOptions(
            compilation.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error));

        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(twae, ct);

        Diagnostic hfc1003 = driver.GetRunResult().Diagnostics.Single(d => d.Id == "HFC1003");
        hfc1003.Severity.ShouldBe(
            DiagnosticSeverity.Error,
            "Under TreatWarningsAsErrors the HFC1003 Warning must break the build.");
    }

    [Fact]
    public void Hfc1003_IsNotDowngraded_AndTwaeIsEnabledRepoWide() {
        // The promotion chain only holds if (1) TWAE is on repo-wide, (2) HFC1003 is absent from the
        // repo NoWarn list, and (3) no .editorconfig rule lowers it. Pin all three so a future config
        // change that would silently let HFC1003 slip through fails this test.
        string repoRoot = GetRepoRoot();

        // (1) Repo-wide TWAE is what promotes the HFC1003 Warning to a build-breaking error.
        string rootProps = File.ReadAllText(Path.Combine(repoRoot, "Directory.Build.props"));
        rootProps.ShouldContain("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");

        // (2) HFC1003 must not be suppressed via NoWarn.
        string srcProps = File.ReadAllText(Path.Combine(repoRoot, "src", "Directory.Build.props"));
        srcProps.ShouldNotContain("HFC1003");

        // (3) .editorconfig must not downgrade HFC1003 below Warning.
        string editorConfig = File.ReadAllText(Path.Combine(repoRoot, ".editorconfig"));
        editorConfig.ShouldNotContain("dotnet_diagnostic.HFC1003");
    }

    private static string GetRepoRoot()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
}
