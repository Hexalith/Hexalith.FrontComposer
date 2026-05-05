using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Incremental;

/// <summary>
/// AC10 / T2 — incremental caching discipline. Adding the drift `AdditionalTextsProvider`
/// must:
/// (a) introduce a new tracked step (expected name <c>LoadDriftBaselines</c>),
/// (b) leave existing tracked steps (`Parse`, `ParseCommand`, `ParseProjectionTemplate`)
///     reporting Cached/Unchanged when the drift baseline content is unchanged,
/// (c) leave existing generated outputs invariant when only an unrelated file edits.
/// </summary>
public sealed class DriftIncrementalCacheTests {
    private const string SkipReason = "RED-PHASE: T2 — drift baseline AdditionalTextsProvider not yet introduced.";

    private const string ExpectedTrackedStepName = "LoadDriftBaselines";

    private const string ValidBaseline = """
        { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
          "algorithm": "frontcomposer-structural-v1",
          "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
            "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
        """;

    [Fact(Skip = SkipReason)]
    public void NewTrackedStep_LoadDriftBaselines_IsRegistered() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(SimpleSource());
        FrontComposerGenerator generator = new();
        AdditionalText baseline = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", ValidBaseline);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baseline],
            optionsProvider: null,
            parseOptions: null,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation, ct);

        GeneratorRunResult result = driver.GetRunResult().Results[0];
        result.TrackedSteps.ContainsKey(ExpectedTrackedStepName).ShouldBeTrue(
            $"AC10 — drift baseline pipeline must register the '{ExpectedTrackedStepName}' tracked step.");
    }

    [Fact(Skip = SkipReason)]
    public void UnchangedBaseline_AndUnrelatedSourceEdit_KeepsExistingParseStepsCached() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(SimpleSource());
        FrontComposerGenerator generator = new();
        AdditionalText baseline = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", ValidBaseline);

        GeneratorDriverOptions trackOpts = new(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baseline],
            optionsProvider: null,
            parseOptions: null,
            driverOptions: trackOpts);
        driver = driver.RunGenerators(compilation, ct);

        SyntaxTree unrelatedTree = CSharpSyntaxTree.ParseText("public class Unrelated { }", cancellationToken: ct);
        CSharpCompilation compilation2 = compilation.AddSyntaxTrees(unrelatedTree);
        driver = driver.RunGenerators(compilation2, ct);

        GeneratorRunResult result = driver.GetRunResult().Results[0];
        AssertCachedOrUnchanged(result, "Parse");
        AssertCachedOrUnchanged(result, ExpectedTrackedStepName);
    }

    [Fact(Skip = SkipReason)]
    public void ChangedBaselineContent_InvalidatesOnlyDriftStep_NotDomainParse() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(SimpleSource());
        FrontComposerGenerator generator = new();
        AdditionalText baselineA = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", ValidBaseline);

        GeneratorDriverOptions trackOpts = new(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineA],
            optionsProvider: null,
            parseOptions: null,
            driverOptions: trackOpts);
        driver = driver.RunGenerators(compilation, ct);

        AdditionalText baselineB = new InMemoryAdditionalText(
            "frontcomposer.drift-baseline.json",
            ValidBaseline.Replace("\"Id\"", "\"OldId\"", StringComparison.Ordinal));
        driver = driver.ReplaceAdditionalTexts(System.Collections.Immutable.ImmutableArray.Create(baselineB));
        driver = driver.RunGenerators(compilation, ct);

        GeneratorRunResult result = driver.GetRunResult().Results[0];
        AssertCachedOrUnchanged(result, "Parse");
        result.TrackedSteps[ExpectedTrackedStepName]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason == IncrementalStepRunReason.Modified)
            .ShouldBeTrue("Changing baseline content must re-run only the drift-baseline step.");
    }

    private static void AssertCachedOrUnchanged(GeneratorRunResult result, string stepName) {
        result.TrackedSteps.ContainsKey(stepName).ShouldBeTrue($"Tracked step '{stepName}' missing.");
        result.TrackedSteps[stepName]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
            .ShouldBeTrue($"AC10 — '{stepName}' should report Cached/Unchanged after non-affecting edit.");
    }

    private static string SimpleSource() => """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace TestDomain;
        [BoundedContext("Orders")]
        [Projection]
        public partial class OrderProjection {
            public string Id { get; set; } = string.Empty;
        }
        """;
}
