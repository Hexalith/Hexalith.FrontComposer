using System.Diagnostics;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Performance;

[Trait("Category", "Performance")]
public class ParseStagePerformanceTests {

    [Fact]
    public void ParseStage_20PlusTypes_CompletesUnder500ms() {
        CancellationToken ct = TestContext.Current.CancellationToken;

        // Generate 25 projection types with diverse field types
        string source = GenerateMultipleProjectionSource(25);
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);

        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        // Warm up
        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult warmupResult = driver.GetRunResult();
        warmupResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        // Sampled measurement: collect median across 5 runs to suppress CI scheduler noise
        const int sampleIterations = 5;
        long[] samples = new long[sampleIterations];
        GeneratorDriverRunResult lastResult = null!;
        for (int i = 0; i < sampleIterations; i++) {
            var sw = Stopwatch.StartNew();
            driver = driver.RunGenerators(compilation, ct);
            sw.Stop();
            samples[i] = sw.ElapsedMilliseconds;
            lastResult = driver.GetRunResult();
        }

        long medianMs = samples.OrderBy(x => x).ElementAt(sampleIterations / 2);
        lastResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        // Parse stage should be well under 50ms; 500ms is the full pipeline budget (NFR8)
        medianMs.ShouldBeLessThan(500,
            $"Parse stage for 25 types took median {medianMs}ms across {sampleIterations} samples [{string.Join(", ", samples)}], exceeding 500ms budget");
    }

    private static string GenerateMultipleProjectionSource(int count) {
        StringBuilder sb = new();
        _ = sb.AppendLine("using System;");
        _ = sb.AppendLine("using System.Collections.Generic;");
        _ = sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        _ = sb.AppendLine("using Hexalith.FrontComposer.Contracts.Attributes;");
        _ = sb.AppendLine();
        _ = sb.AppendLine("namespace TestDomain.Performance;");
        _ = sb.AppendLine();

        // Shared enum for badge mapping
        _ = sb.AppendLine("public enum ItemStatus");
        _ = sb.AppendLine("{");
        _ = sb.AppendLine("    [ProjectionBadge(BadgeSlot.Success)] Active,");
        _ = sb.AppendLine("    [ProjectionBadge(BadgeSlot.Warning)] Inactive,");
        _ = sb.AppendLine("    [ProjectionBadge(BadgeSlot.Danger)] Archived,");
        _ = sb.AppendLine("}");
        _ = sb.AppendLine();

        for (int i = 0; i < count; i++) {
            _ = sb.AppendLine("[BoundedContext(\"Perf\")]");
            _ = sb.AppendLine("[Projection]");
            _ = sb.AppendLine("[ProjectionRole(ProjectionRole.StatusOverview)]");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"public partial class Projection{i}");
            _ = sb.AppendLine("{");
            _ = sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    [Display(Name = \"Name {i}\")]");
            _ = sb.AppendLine("    public string Name { get; set; } = string.Empty;");
            _ = sb.AppendLine("    public int Count { get; set; }");
            _ = sb.AppendLine("    public decimal Amount { get; set; }");
            _ = sb.AppendLine("    public bool IsActive { get; set; }");
            _ = sb.AppendLine("    public DateTime CreatedAt { get; set; }");
            _ = sb.AppendLine("    public Guid Id { get; set; }");
            _ = sb.AppendLine("    public ItemStatus Status { get; set; }");
            _ = sb.AppendLine("    public string? Description { get; set; }");
            _ = sb.AppendLine("    public int? OptionalCount { get; set; }");
            _ = sb.AppendLine("    public List<string> Tags { get; set; } = new();");
            _ = sb.AppendLine("}");
            _ = sb.AppendLine();
        }

        return sb.ToString();
    }
}
