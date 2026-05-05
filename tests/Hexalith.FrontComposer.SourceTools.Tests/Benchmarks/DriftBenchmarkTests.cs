using System.Diagnostics;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Benchmarks;

/// <summary>
/// AC11 / T7 — NFR8: median incremental added overhead for drift detection on a representative
/// bounded fixture must remain &lt; 500 ms. Warmup excluded, ≥ 20 measured iterations, median
/// and p95 reported for both cache-hit and cache-miss paths. Sibling to
/// <c>IncrementalRebuildBenchmarkTests</c>; marked benchmark, not a unit gate.
/// </summary>
public sealed class DriftBenchmarkTests {
    private const string SkipReason = "RED-PHASE: T7 — drift benchmark harness pending; perf machine-sensitive, kept out of unit gate.";

    private const int WarmupIterations = 5;
    private const int MeasuredIterations = 20;
    private const double Nfr8MedianBudgetMs = 500.0;

    [Fact(Skip = SkipReason)]
    public void IncrementalRebuild_WithDriftDetection_StaysBelowNfr8MedianBudget_CacheHit() {
        (CSharpCompilation compilation, AdditionalText baseline) = BuildBoundedFixture(declarationCount: 25);
        FrontComposerGenerator generator = new();

        // Prime: warm-up runs that do not contribute to measurement.
        GeneratorDriver primed = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()], additionalTexts: [baseline]);
        for (int i = 0; i < WarmupIterations; i++) {
            primed = primed.RunGenerators(compilation, TestContext.Current.CancellationToken);
        }

        // Measured: cache-hit path = re-run with identical inputs.
        double[] cacheHitMs = new double[MeasuredIterations];
        for (int i = 0; i < MeasuredIterations; i++) {
            Stopwatch sw = Stopwatch.StartNew();
            primed = primed.RunGenerators(compilation, TestContext.Current.CancellationToken);
            sw.Stop();
            cacheHitMs[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(cacheHitMs);
        double medianMs = cacheHitMs[MeasuredIterations / 2];
        double p95Ms = cacheHitMs[(int)(MeasuredIterations * 0.95)];

        medianMs.ShouldBeLessThan(Nfr8MedianBudgetMs,
            $"AC11 / NFR8 — median cache-hit incremental added overhead {medianMs:F1} ms exceeded {Nfr8MedianBudgetMs} ms budget.");
        p95Ms.ShouldBeLessThan(Nfr8MedianBudgetMs * 2.0,
            $"AC11 — p95 cache-hit overhead {p95Ms:F1} ms exceeded soft 2x budget.");
    }

    [Fact(Skip = SkipReason)]
    public void IncrementalRebuild_WithDriftDetection_StaysBelowNfr8MedianBudget_CacheMiss() {
        FrontComposerGenerator generator = new();
        (CSharpCompilation baseCompilation, AdditionalText baseline) = BuildBoundedFixture(declarationCount: 25);

        GeneratorDriver primed = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()], additionalTexts: [baseline]);
        for (int i = 0; i < WarmupIterations; i++) {
            primed = primed.RunGenerators(baseCompilation, TestContext.Current.CancellationToken);
        }

        double[] cacheMissMs = new double[MeasuredIterations];
        for (int i = 0; i < MeasuredIterations; i++) {
            // Cache-miss path = mutate ONE projection per iteration to trigger re-comparison.
            CSharpCompilation mutated = MutateOneDeclaration(baseCompilation, iteration: i);
            Stopwatch sw = Stopwatch.StartNew();
            primed = primed.RunGenerators(mutated, TestContext.Current.CancellationToken);
            sw.Stop();
            cacheMissMs[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(cacheMissMs);
        double medianMs = cacheMissMs[MeasuredIterations / 2];

        medianMs.ShouldBeLessThan(Nfr8MedianBudgetMs,
            $"AC11 / NFR8 — median cache-miss incremental added overhead {medianMs:F1} ms exceeded {Nfr8MedianBudgetMs} ms budget.");
    }

    private static (CSharpCompilation compilation, AdditionalText baseline) BuildBoundedFixture(int declarationCount) {
        StringBuilder source = new();
        source.AppendLine("using Hexalith.FrontComposer.Contracts.Attributes;");
        source.AppendLine("namespace TestDomain;");
        StringBuilder baselineSb = new();
        baselineSb.AppendLine("{ \"schemaVersion\": \"frontcomposer.generated-ui-baseline.v1\", \"algorithm\": \"frontcomposer-structural-v1\", \"contracts\": [");

        for (int i = 0; i < declarationCount; i++) {
            source.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                "[BoundedContext(\"Orders\")] [Projection] public partial class P{0:D2} {{ public string Id {{ get; set; }} = string.Empty; public int Priority {{ get; set; }} }}{1}",
                i, Environment.NewLine);
            baselineSb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                "{0}{{ \"family\": \"projection\", \"type\": \"TestDomain.P{1:D2}\", \"boundedContext\": \"Orders\", \"properties\": [{{ \"name\": \"Id\", \"category\": \"String\", \"nullable\": false }}, {{ \"name\": \"Priority\", \"category\": \"Int32\", \"nullable\": false }}] }}",
                i == 0 ? string.Empty : ",\n", i);
        }
        baselineSb.AppendLine();
        baselineSb.AppendLine("] }");

        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source.ToString());
        AdditionalText baseline = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineSb.ToString());
        return (compilation, baseline);
    }

    private static CSharpCompilation MutateOneDeclaration(CSharpCompilation compilation, int iteration) {
        SyntaxTree existing = compilation.SyntaxTrees.First();
        string mutated = existing.ToString().Replace(
            "public int Priority",
            iteration % 2 == 0 ? "public int? Priority" : "public int Priority",
            StringComparison.Ordinal);
        return compilation.ReplaceSyntaxTree(existing, CSharpSyntaxTree.ParseText(mutated));
    }
}
