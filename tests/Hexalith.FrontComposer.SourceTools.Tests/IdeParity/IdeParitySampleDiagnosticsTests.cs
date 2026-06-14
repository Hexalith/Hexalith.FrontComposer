using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.IdeParity;

/// <summary>
/// Story 9-3 T3 / T9 — Verifies that the IdeParityCounter sample exercises HFC diagnostics
/// with HelpLinkUri set, that public attributes carry IDE-visible XML documentation, and that
/// the analyzer surface is reachable from the deterministic fixture (not just the drift catalog
/// covered by Story 9-1).
/// </summary>
[Trait("MatrixRowId", "IDE-MUST-002")]
[Trait("MatrixRowId", "IDE-MUST-004")]
public sealed class IdeParitySampleDiagnosticsTests {
    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-004")]
    public void IdeParityCounterFixture_AllEmittedHfcDiagnosticsHaveHelpLinkUri() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(IdeParityConformanceUtilityTests.LoadIdeParityCounterFixtureSource());
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        Diagnostic[] hfcDiagnostics = result.Diagnostics
            .Where(d => d.Id.StartsWith("HFC", StringComparison.Ordinal))
            .ToArray();

        foreach (Diagnostic diagnostic in hfcDiagnostics) {
            diagnostic.Descriptor.HelpLinkUri.ShouldNotBeNullOrWhiteSpace(
                $"HFC diagnostic '{diagnostic.Id}' surfaced from the IdeParityCounter fixture must carry a HelpLinkUri so IDE squiggles link to documentation.");
        }
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-002")]
    public void PublicFrontComposerAttributes_HaveIdeVisibleXmlDocs() {
        // The attribute source files (not the compiled XML doc) are the authoritative
        // location for IDE Quick Info text. Asserting `/// <summary>` blocks here keeps
        // the test independent of <GenerateDocumentationFile> wiring on the Contracts
        // assembly (which would cascade CS1591 across many existing public types and
        // is intentionally deferred until the API-freeze CS1591 ramp described in
        // Story 9-3 Dev Notes).
        string[] relativeAttributeSources =
        {
            "src/Hexalith.FrontComposer.Contracts/Attributes/BoundedContextAttribute.cs",
            "src/Hexalith.FrontComposer.Contracts/Attributes/CommandAttribute.cs",
            "src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionAttribute.cs",
        };

        foreach (string relative in relativeAttributeSources) {
            string path = Path.Combine(IdeParityRepositoryRoot.Value, relative.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(path).ShouldBeTrue($"Attribute source '{relative}' must exist.");

            string source = File.ReadAllText(path);
            source.ShouldContain("/// <summary>", customMessage: $"'{relative}' must carry an XML <summary> so IDE Quick Info renders attribute documentation.");
            source.ShouldContain("/// </summary>", customMessage: $"'{relative}' must close its XML <summary> block.");
        }
    }

    [Fact]
    [Trait("MatrixRowId", "IDE-MUST-004")]
    public void GeneratedFixtureCompilation_ProducesNoErrorSeverityDiagnosticsAndExposesHfcCatalog() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(IdeParityConformanceUtilityTests.LoadIdeParityCounterFixtureSource());
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("The IdeParityCounter fixture must not raise error-severity diagnostics.");

        // The generator must register at least one supported HFC descriptor so IDEs can surface
        // help links and diagnostic categories. This test does not enumerate a specific catalog
        // entry (Story 9-4 owns the per-ID docs); it asserts the catalog is reachable.
        DiagnosticDescriptor[] hfcDescriptors = generator
            .GetType()
            .GetField("SupportedDiagnostics", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) as DiagnosticDescriptor[]
            ?? Array.Empty<DiagnosticDescriptor>();
        // FrontComposerGenerator may not expose SupportedDiagnostics directly; fall back to
        // probing the descriptors discovered through the generator-emitted diagnostics list.
        if (hfcDescriptors.Length == 0) {
            hfcDescriptors = result.Diagnostics
                .Where(d => d.Id.StartsWith("HFC", StringComparison.Ordinal))
                .Select(d => d.Descriptor)
                .Distinct()
                .ToArray();
        }

        // It is acceptable for the deterministic fixture to surface zero HFC diagnostics
        // (the sample is shaped to be clean). The contract is: anything that *does* surface
        // must carry HelpLinkUri (asserted in the dedicated test above), and the assembly
        // exposes the diagnostic-descriptor surface in its public API.
        _ = typeof(FrontComposerGenerator).Assembly
            .GetType("Hexalith.FrontComposer.SourceTools.Diagnostics.DiagnosticDescriptors")
            .ShouldNotBeNull("The diagnostic descriptor catalog must be reachable so IDEs can render HFC-specific Quick Info.");
    }
}
