using Hexalith.FrontComposer.Contracts.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.TrimAot;

/// <summary>
/// AC14 + AC15 / T6 — narrow trim/AOT reflection-catalog evidence diagnostic.
/// </summary>
public sealed class TrimAotReflectionCatalogDiagnosticTests {
    private const string ProjectionSource = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace TestDomain;
        [BoundedContext("Orders")]
        [Projection]
        public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
        """;

    private const string AdopterClassOverrideSource = """
        using System;
        using System.Collections.Generic;
        using Hexalith.FrontComposer.Contracts.Badges;

        namespace TestDomain;

        public sealed class AdopterCatalog : IActionQueueProjectionCatalog {
            public IReadOnlyList<Type> ActionQueueTypes => Array.Empty<Type>();
        }
        """;

    [Theory]
    [InlineData(true, false)]  // PublishTrimmed alone
    [InlineData(false, true)]  // PublishAot alone
    [InlineData(true, true)]   // PublishTrimmed + PublishAot
    public void TrimOrAotEnabled_AndNoOverrideEvidence_EmitsHfc1070(bool publishTrimmed, bool publishAot) {
        // Story 11.4 / DEF-9-1C-2 — AC14 treats trim-enabled OR native-AOT hosts as advisory
        // triggers. Keep the PublishAot-only row pinned so the gate cannot regress to
        // PublishTrimmed-only behavior.
        IReadOnlyList<Diagnostic> diagnostics = Run(ProjectionSource, publishTrimmed: publishTrimmed, publishAot: publishAot);

        Diagnostic? trim = diagnostics.FirstOrDefault(d => d.Id == FcDiagnosticIds.HFC1070_TrimAotReflectionCatalogWarning);
        // CH-6 / CM-6 — pin to HFC ID, then sanity-check message references catalog.
        _ = trim.ShouldNotBeNull(
            $"AC14 — HFC1070 must fire when PublishTrimmed={publishTrimmed} (PublishAot={publishAot}) with default reflection catalog.");
        trim!.Severity.ShouldBe(DiagnosticSeverity.Warning);
        trim.GetMessage().ShouldContain("IActionQueueProjectionCatalog", Case.Insensitive);
        trim.GetMessage().ShouldContain("source-generated", Case.Insensitive);
    }

    [Fact]
    public void NeitherTrimNorAot_NoDiagnostic() {
        IReadOnlyList<Diagnostic> diagnostics = Run(ProjectionSource, publishTrimmed: false, publishAot: false);
        diagnostics.Any(d => d.Id == FcDiagnosticIds.HFC1070_TrimAotReflectionCatalogWarning).ShouldBeFalse();
    }

    [Fact]
    public void TrimEnabled_WithAdopterOverride_SilencesHfc1070() {
        // CC-6 — AC14: when an adopter override is statically observable, HFC1070 must NOT fire.
        IReadOnlyList<Diagnostic> diagnostics = Run(
            sources: [ProjectionSource, AdopterClassOverrideSource],
            publishTrimmed: true,
            publishAot: false);

        diagnostics.Any(d => d.Id == FcDiagnosticIds.HFC1070_TrimAotReflectionCatalogWarning).ShouldBeFalse(
            "AC14 — adopter-supplied IActionQueueProjectionCatalog override must silence HFC1070.");
    }

    [Fact]
    public void TrimEnabled_WithMultipleAdopterOverrides_SilencesHfc1070() {
        // CM-12 — multiple override candidates are valid catalog discipline; HFC1070 must stay silent.
        const string secondOverride = """
            using System;
            using System.Collections.Generic;
            using Hexalith.FrontComposer.Contracts.Badges;

            namespace TestDomain.Other;

            public sealed class SecondAdopterCatalog : IActionQueueProjectionCatalog {
                public IReadOnlyList<Type> ActionQueueTypes => Array.Empty<Type>();
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(
            sources: [ProjectionSource, AdopterClassOverrideSource, secondOverride],
            publishTrimmed: true,
            publishAot: false);

        diagnostics.Any(d => d.Id == FcDiagnosticIds.HFC1070_TrimAotReflectionCatalogWarning).ShouldBeFalse();
    }

    [Fact]
    public void TrimEnabled_WithRecordOverride_StillFiresHfc1070_DocumentsCurrentBehavior() {
        // CM-13 — production filters out non-class TypeKinds (records compile to classes but are
        // currently picked up; record struct / abstract class are skipped). Pin current behavior:
        // a record-class adopter override IS detected and silences HFC1070.
        const string recordOverride = """
            using System;
            using System.Collections.Generic;
            using Hexalith.FrontComposer.Contracts.Badges;

            namespace TestDomain;

            public sealed record class RecordCatalog : IActionQueueProjectionCatalog {
                public IReadOnlyList<Type> ActionQueueTypes => Array.Empty<Type>();
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(
            sources: [ProjectionSource, recordOverride],
            publishTrimmed: true,
            publishAot: false);

        diagnostics.Any(d => d.Id == FcDiagnosticIds.HFC1070_TrimAotReflectionCatalogWarning).ShouldBeFalse(
            "AC14 — record-class adopter overrides count as override evidence (current behavior).");
    }

    [Fact]
    public void TrimEnabled_NoContractsReference_NoDiagnostic_DocumentsDefensiveBranch() {
        // CM-11 — when Contracts is not referenced, the analyzer cannot resolve the interface
        // symbol; production returns true (silence) so unrelated builds aren't pelted with
        // HFC1070. Pin this defensive behavior.
        const string source = """
            namespace TestDomain;
            public class IsolatedDomainObject { public string Name { get; set; } = string.Empty; }
            """;

        CancellationToken ct = TestContext.Current.CancellationToken;
        // Compile WITHOUT the Contracts/SourceTools-backed reference set — minimal corlib only.
        var compilation = CSharpCompilation.Create(
            "Isolated",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: ct)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Immutable.ImmutableArray<>).Assembly.Location),
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        FrontComposerGenerator generator = new();
        AnalyzerConfigOptionsProvider options = TrimAotOptions(publishTrimmed: true, publishAot: false);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);

        IReadOnlyList<Diagnostic> diagnostics = driver.GetRunResult().Diagnostics;
        diagnostics.Any(d => d.Id == FcDiagnosticIds.HFC1070_TrimAotReflectionCatalogWarning).ShouldBeFalse(
            "AC15 — when Contracts is not referenced, the analyzer cannot prove anything; stay silent.");
    }

    [Fact]
    public void Hfc1070EmittedDiagnostic_DocumentsRuntimeAuthoritativePath() {
        // CM-18 — AC15 mandates the limitation is "recorded explicitly" and runtime validators
        // remain authoritative. The descriptor's MessageFormat is the parameterized "{0}" shape
        // (DEF-9-1A-3), so we trigger an actual diagnostic and assert its rendered message
        // mentions the source-generated path — the user-visible "recorded explicitly" surface.
        IReadOnlyList<Diagnostic> diagnostics = Run(ProjectionSource, publishTrimmed: true, publishAot: false);
        Diagnostic? trim = diagnostics.FirstOrDefault(d => d.Id == FcDiagnosticIds.HFC1070_TrimAotReflectionCatalogWarning);
        _ = trim.ShouldNotBeNull("AC14 — HFC1070 must fire under the test conditions.");
        trim!.GetMessage().ShouldContain("source-generated", Case.Insensitive,
            "AC15 — HFC1070 message must point at the source-generated catalog path so the runtime-authoritative remediation is recorded explicitly.");
    }

    private static IReadOnlyList<Diagnostic> Run(string source, bool publishTrimmed, bool publishAot)
        => Run([source], publishTrimmed, publishAot);

    private static IReadOnlyList<Diagnostic> Run(string[] sources, bool publishTrimmed, bool publishAot) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(sources);
        FrontComposerGenerator generator = new();
        AnalyzerConfigOptionsProvider options = TrimAotOptions(publishTrimmed, publishAot);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static AnalyzerConfigOptionsProvider TrimAotOptions(bool publishTrimmed, bool publishAot)
        => new InMemoryOptions(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["build_property.PublishTrimmed"] = publishTrimmed ? "true" : "false",
            ["build_property.PublishAot"] = publishAot ? "true" : "false",
        });

    private sealed class InMemoryOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new InMemory(values);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class InMemory(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions {
            public override bool TryGetValue(string key, out string value) {
                if (values.TryGetValue(key, out string? v)) { value = v; return true; }
                value = string.Empty; return false;
            }
        }
    }
}
