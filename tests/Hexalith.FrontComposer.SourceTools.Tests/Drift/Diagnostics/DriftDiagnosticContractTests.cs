using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Diagnostics;

/// <summary>
/// AC12 / T4 — diagnostic contract. Each drift diagnostic must:
/// (a) carry a populated <see cref="DiagnosticDescriptor.HelpLinkUri"/>,
/// (b) expose all stable property keys (BaselinePath, DeclarationPath, DeclarationName,
///     MemberName, DriftKind, ExpectedShapeHash, ActualShapeHash, SchemaVersion, AlgorithmVersion),
/// (c) include the What/Expected/Got/Fix/DocsLink narrative shape in its message,
/// (d) normalize paths to repo-relative forward slashes or the &lt;outside-project&gt; sentinel.
/// </summary>
public sealed class DriftDiagnosticContractTests {
    private const string SkipReason = "RED-PHASE: T4 — drift diagnostic contract not yet introduced.";

    private static readonly string[] RequiredPropertyKeys = [
        "BaselinePath",
        "DeclarationPath",
        "DeclarationName",
        "MemberName",
        "DriftKind",
        "ExpectedShapeHash",
        "ActualShapeHash",
        "SchemaVersion",
        "AlgorithmVersion",
    ];

    private const string BaselineProjectionRemovedMember = """
        { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
          "algorithm": "frontcomposer-structural-v1",
          "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
            "properties": [
              { "name": "Id",      "category": "String", "nullable": false, "columnPriority": 0 },
              { "name": "Removed", "category": "String", "nullable": false, "columnPriority": 1 }
            ] }] }
        """;

    [Fact()]
    public void EveryDriftDiagnostic_CarriesPopulatedHelpLinkUri() {
        IReadOnlyList<Diagnostic> diagnostics = Run(SourceWithMissingMember(), BaselineProjectionRemovedMember);

        Diagnostic[] driftDiagnostics = [.. diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal))];
        driftDiagnostics.Length.ShouldBeGreaterThan(0);
        foreach (Diagnostic d in driftDiagnostics) {
            d.Descriptor.HelpLinkUri.ShouldNotBeNullOrWhiteSpace($"AC12 — {d.Id} must populate HelpLinkUri.");
            d.Descriptor.HelpLinkUri.ShouldStartWith("https://hexalith.github.io/FrontComposer/diagnostics/");
        }
    }

    [Fact()]
    public void EveryDriftDiagnostic_ExposesAllRequiredPropertyKeys_OrSentinel() {
        IReadOnlyList<Diagnostic> diagnostics = Run(SourceWithMissingMember(), BaselineProjectionRemovedMember);

        Diagnostic[] driftDiagnostics = [.. diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal))];
        driftDiagnostics.Length.ShouldBeGreaterThan(0);
        foreach (Diagnostic d in driftDiagnostics) {
            foreach (string key in RequiredPropertyKeys) {
                d.Properties.ContainsKey(key).ShouldBeTrue($"AC12 — {d.Id} missing property '{key}'.");
                // Absent values must use a fixed sentinel, never null/empty.
                d.Properties[key].ShouldNotBeNullOrWhiteSpace($"AC12 — {d.Id} property '{key}' must be a stable value or sentinel.");
            }
        }
    }

    [Fact()]
    public void DiagnosticMessage_HasWhatExpectedGotFixDocsLinkShape() {
        IReadOnlyList<Diagnostic> diagnostics = Run(SourceWithMissingMember(), BaselineProjectionRemovedMember);

        Diagnostic? drift = diagnostics.FirstOrDefault(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                                                         && d.GetMessage().Contains("Removed", StringComparison.Ordinal));
        drift.ShouldNotBeNull();
        string message = drift!.GetMessage();
        // Story 9-1 review CB-6: AC12 enumerates "What, Expected, Got, Fix, and DocsLink fields".
        // Production templates do emit "What:" — assert it explicitly so a regression that drops
        // the leading What: clause is caught.
        message.ShouldContain("What",     Case.Insensitive);
        message.ShouldContain("Expected", Case.Insensitive);
        message.ShouldContain("Got",      Case.Insensitive);
        message.ShouldContain("Fix",      Case.Insensitive);
        message.ShouldContain("https://hexalith.github.io/FrontComposer/diagnostics/", Case.Insensitive);
    }

    [Fact()]
    public void BaselinePathProperty_IsRepoRelativeForwardSlash_OrOutsideProjectSentinel() {
        IReadOnlyList<Diagnostic> diagnostics = Run(SourceWithMissingMember(), BaselineProjectionRemovedMember);

        Diagnostic[] driftDiagnostics = [.. diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal))];
        foreach (Diagnostic d in driftDiagnostics) {
            string baselinePath = d.Properties["BaselinePath"]!;
            baselinePath.ShouldNotContain("\\", customMessage: "AC12 — BaselinePath must be forward-slash.");
            IsWindowsDriveRootedPath(baselinePath).ShouldBeFalse(
                "AC12 — BaselinePath must not leak a Windows drive-root absolute path.");
            (baselinePath == "<outside-project>" || !Path.IsPathRooted(baselinePath))
                .ShouldBeTrue("AC12 — BaselinePath must be repo-relative or the <outside-project> sentinel.");
        }
    }

    [Theory]
    [InlineData("fixtures/frontcomposer:drift-baseline.json", false)]
    [InlineData("C:/repo/frontcomposer.drift-baseline.json", true)]
    [InlineData("C:\\repo\\frontcomposer.drift-baseline.json", true)]
    [InlineData("C:repo/frontcomposer.drift-baseline.json", false)]
    public void WindowsDriveRootedPathCheck_AllowsBenignColonOnly(string path, bool expected) {
        path.ShouldNotBeNull();
        IsWindowsDriveRootedPath(path).ShouldBe(expected);
    }

    [Fact()]
    public void DiagnosticLocation_PointsAtSourceDeclaration_NotBaselineFile() {
        IReadOnlyList<Diagnostic> diagnostics = Run(SourceWithMissingMember(), BaselineProjectionRemovedMember);

        Diagnostic[] driftDiagnostics = [.. diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                                                                && d.Location != Location.None)];
        driftDiagnostics.Length.ShouldBeGreaterThan(0);
        foreach (Diagnostic d in driftDiagnostics) {
            // Story 9-1 review P12: drift comparison no longer combines `CompilationProvider`
            // (so SyntaxTree-backed locations are not available without re-coupling), but the
            // diagnostic location still carries the C# file path via `GetMappedLineSpan().Path`.
            // Story 9-1 review CB-4: a path of `"baseline-stub.cs"` would slip past the
            // earlier `EndsWith(".cs")`-only check. Pin the path to the actual source file
            // (`Test0.cs` is the synthetic name produced by `CompilationHelper.CreateCompilation`)
            // and assert the line/column references the projection declaration position.
            Microsoft.CodeAnalysis.FileLinePositionSpan span = d.Location.GetMappedLineSpan();
            string path = span.Path;
            path.ShouldEndWith("Test0.cs",
                customMessage: "AC12 — diagnostic location must point at the source `.cs` file, not a baseline-side stub. Got: " + path);
            path.ShouldNotContain("baseline",
                customMessage: "AC12 — diagnostic location must NOT reference the baseline JSON.");
            // Synthetic test compilation produces declarations starting at line 0 (1-indexed: 1+).
            (span.StartLinePosition.Line >= 0).ShouldBeTrue("AC12 — location must carry a real line position.");
        }
    }

    private static string SourceWithMissingMember() => """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace TestDomain;
        [BoundedContext("Orders")]
        [Projection]
        public partial class OrderProjection {
            public string Id { get; set; } = string.Empty;
        }
        """;

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions());
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static bool IsWindowsDriveRootedPath(string path)
        => path.Length >= 3
            && ((path[0] >= 'A' && path[0] <= 'Z') || (path[0] >= 'a' && path[0] <= 'z'))
            && path[1] == ':'
            && (path[2] == '/' || path[2] == '\\');
}
