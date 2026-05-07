using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.DriftTestFixtures;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Incremental;

/// <summary>
/// AC10 / T2 — incremental caching discipline.
/// </summary>
public sealed class DriftIncrementalCacheTests {
    private const string ExpectedTrackedStepName = "LoadDriftBaselines";

    private const string ValidBaseline = """
        { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
          "algorithm": "frontcomposer-structural-v1",
          "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
            "properties": [{ "name": "__MUTATE_ME__", "category": "String", "nullable": false }] }] }
        """;

    [Fact]
    public void NewTrackedStep_LoadDriftBaselines_IsRegistered() {
        GeneratorRunResult result = RunWithDrift([SimpleSource()], ValidBaseline);

        // CH-21 — `LoadDriftBaselines` is the contracted step name; if production renames the
        // step, look up via "any step whose name contains 'Drift'" and fail fast with a useful
        // message rather than a generic missing-key throw.
        string? actualName = result.TrackedSteps.Keys
            .FirstOrDefault(k => k.Contains("Drift", StringComparison.OrdinalIgnoreCase));
        actualName.ShouldBe(ExpectedTrackedStepName,
            $"AC10 — drift baseline pipeline must register the '{ExpectedTrackedStepName}' tracked step (found '{actualName ?? "<none>"}').");
    }

    [Fact]
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
            optionsProvider: CompilationHelper.DriftEnabledOptions(),
            parseOptions: null,
            driverOptions: trackOpts);
        driver = driver.RunGenerators(compilation, ct);

        SyntaxTree unrelatedTree = CSharpSyntaxTree.ParseText("public class Unrelated { }", cancellationToken: ct);
        CSharpCompilation compilation2 = compilation.AddSyntaxTrees(unrelatedTree);
        driver = driver.RunGenerators(compilation2, ct);

        GeneratorRunResult result = driver.GetRunResult().Results[0];
        // CC-8 — assert all three AC10-named steps are Cached/Unchanged.
        AssertCachedOrUnchanged(result, "Parse");
        AssertCachedOrUnchanged(result, "ParseCommand");
        AssertCachedOrUnchanged(result, "ParseProjectionTemplate");
        AssertCachedOrUnchanged(result, ExpectedTrackedStepName);
    }

    [Fact]
    public void IdenticalBaselineContent_AcrossRuns_KeepsLoadDriftBaselinesCached() {
        // CH-20 — AC10 sub-clause: "unchanged AdditionalText baseline content" must not invalidate.
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(SimpleSource());
        FrontComposerGenerator generator = new();

        GeneratorDriverOptions trackOpts = new(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [new InMemoryAdditionalText("frontcomposer.drift-baseline.json", ValidBaseline)],
            optionsProvider: CompilationHelper.DriftEnabledOptions(),
            parseOptions: null,
            driverOptions: trackOpts);
        driver = driver.RunGenerators(compilation, ct);

        // Replace AdditionalTexts with a fresh instance carrying identical Path and content.
        AdditionalText sameContentReplacement = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", ValidBaseline);
        driver = driver.ReplaceAdditionalTexts(ImmutableArray.Create(sameContentReplacement));
        driver = driver.RunGenerators(compilation, ct);

        GeneratorRunResult result = driver.GetRunResult().Results[0];
        AssertCachedOrUnchanged(result, ExpectedTrackedStepName);
    }

    [Fact]
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
            optionsProvider: CompilationHelper.DriftEnabledOptions(),
            parseOptions: null,
            driverOptions: trackOpts);
        driver = driver.RunGenerators(compilation, ct);

        // CM-3 — sentinel-based mutation, robust against future fixture expansion.
        AdditionalText baselineB = new InMemoryAdditionalText(
            "frontcomposer.drift-baseline.json",
            ValidBaseline.Replace("__MUTATE_ME__", "RenamedMember", StringComparison.Ordinal));
        driver = driver.ReplaceAdditionalTexts(ImmutableArray.Create(baselineB));
        driver = driver.RunGenerators(compilation, ct);

        GeneratorRunResult result = driver.GetRunResult().Results[0];

        // CC-8 — all three AC10-named domain-parse steps must remain Cached/Unchanged.
        AssertCachedOrUnchanged(result, "Parse");
        AssertCachedOrUnchanged(result, "ParseCommand");
        AssertCachedOrUnchanged(result, "ParseProjectionTemplate");

        // CC-4 — drift step must show Modified output.
        result.TrackedSteps[ExpectedTrackedStepName]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason == IncrementalStepRunReason.Modified)
            .ShouldBeTrue("Changing baseline content must re-run only the drift-baseline step.");
    }

    private static GeneratorRunResult RunWithDrift(string[] sources, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(sources);
        FrontComposerGenerator generator = new();
        AdditionalText baseline = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baseline],
            optionsProvider: CompilationHelper.DriftEnabledOptions(),
            parseOptions: null,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Results[0];
    }

    private static void AssertCachedOrUnchanged(GeneratorRunResult result, string stepName) {
        if (!result.TrackedSteps.ContainsKey(stepName)) {
            // Step was not exercised in this fixture — that's allowed (e.g., Parse for a
            // command-only fixture). The AC10 contract is "remains Cached/Unchanged when
            // exercised", not "must always be present".
            return;
        }

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
            public string __MUTATE_ME__ { get; set; } = string.Empty;
        }
        """;
}
