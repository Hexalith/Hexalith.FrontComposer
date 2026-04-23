using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Incremental;

/// <summary>
/// Story 4-1 T1.8 — Roslyn incremental-generator cache-hit checks: identical inputs must
/// reuse cached outputs; <c>WhenState</c> edits must re-run Parse. Per Winston's review —
/// without this, D19 silent-stale-emit regressions are invisible to every other
/// cache-equality test.
/// </summary>
public class IncrementalGeneratorCacheHitTests {
    [Fact]
    public void SameInputTwiceProducesCachedSteps() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = BuildProjection("Pending,Submitted");

        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation, ct);
        string firstGeneratedOutput = SerializeGeneratedSources(driver.GetRunResult().Results[0]);
        driver = driver.RunGenerators(compilation, ct);

        GeneratorRunResult result = driver.GetRunResult().Results[0];
        string secondGeneratedOutput = SerializeGeneratedSources(result);

        result.TrackedSteps.ContainsKey("Parse").ShouldBeTrue();

        bool allCached = result.TrackedSteps["Parse"]
            .SelectMany(s => s.Outputs)
            .All(o => o.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);

        allCached.ShouldBeTrue(
            "Running the generator twice on the identical compilation must reuse Parse-stage outputs.");
        secondGeneratedOutput.ShouldBe(
            firstGeneratedOutput,
            "Running the generator twice on the identical compilation must also preserve the generated output set.");
    }

    [Fact]
    public void EditingWhenStateInvalidatesCache() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source1 = BuildProjection("Pending");
        string source2 = BuildProjection("Pending,Submitted");

        CSharpCompilation compilation1 = CompilationHelper.CreateCompilation(source1);
        FrontComposerGenerator generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation1, ct);
        string firstGeneratedOutput = SerializeGeneratedSources(driver.GetRunResult().Results[0]);

        CSharpCompilation compilation2 = compilation1.ReplaceSyntaxTree(
            compilation1.SyntaxTrees.First(),
            CSharpSyntaxTree.ParseText(cancellationToken: ct, text: source2));
        driver = driver.RunGenerators(compilation2, ct);

        GeneratorRunResult result2 = driver.GetRunResult().Results[0];
        string secondGeneratedOutput = SerializeGeneratedSources(result2);

        bool parseReran = result2.TrackedSteps["Parse"]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New);

        parseReran.ShouldBeTrue(
            "Editing WhenState must invalidate the Parse-stage cache — otherwise the generator emits stale output.");
        secondGeneratedOutput.ShouldNotBe(
            firstGeneratedOutput,
            "Editing WhenState must also produce a different generated output set.");
        secondGeneratedOutput.ShouldContain("Submitted");
    }

    private static string SerializeGeneratedSources(GeneratorRunResult result) {
        StringBuilder sb = new();
        foreach (GeneratedSourceResult source in result.GeneratedSources.OrderBy(s => s.HintName, StringComparer.Ordinal)) {
            _ = sb.AppendLine(source.HintName);
            _ = sb.AppendLine(source.SourceText.ToString());
        }

        return sb.ToString();
    }

    private static string BuildProjection(string whenState) => $@"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Orders"")]
[Projection]
[ProjectionRole(ProjectionRole.ActionQueue, WhenState = ""{whenState}"")]
public partial class CacheTestProjection
{{
    public string Id {{ get; set; }} = string.Empty;
    public OrderStatus Status {{ get; set; }}
}}

public enum OrderStatus
{{
    Pending,
    Submitted,
    Approved,
}}";
}
