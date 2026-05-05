using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison;

/// <summary>
/// AC7 / T3 — display/role/group/priority/badge/icon/destructive/policy metadata drift is
/// classified by impact category. The bound is at most one diagnostic per declaration per
/// impact category, even when many attributes inside that category change at once.
/// </summary>
public sealed class DriftClassifierMetadataTests {
    private const string SkipReason = "RED-PHASE: T3 — metadata drift classifier not yet introduced.";

    [Theory(Skip = SkipReason)]
    [InlineData("Display.Name",                   "[System.ComponentModel.DataAnnotations.Display(Name=\"OldLabel\")]", "[System.ComponentModel.DataAnnotations.Display(Name=\"NewLabel\")]")]
    [InlineData("Display.GroupName",              "[System.ComponentModel.DataAnnotations.Display(GroupName=\"GroupA\")]", "[System.ComponentModel.DataAnnotations.Display(GroupName=\"GroupB\")]")]
    [InlineData("Description",                    "[System.ComponentModel.Description(\"Old\")]",                          "[System.ComponentModel.Description(\"New\")]")]
    [InlineData("ColumnPriority",                 "[ColumnPriority(1)]",                                                   "[ColumnPriority(7)]")]
    [InlineData("ProjectionFieldGroup",           "[ProjectionFieldGroup(\"Schedule\")]",                                  "[ProjectionFieldGroup(\"Logistics\")]")]
    public void DisplayMetadataChange_EmitsAtMostOneDiagnosticPerDeclarationPerCategory(string category, string oldAttr, string newAttr) {
        string source = $$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                {{newAttr}}
                public string Notes { get; set; } = string.Empty;
            }
            """;
        string baseline = $$"""
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [
                  { "name": "Id",    "category": "String", "nullable": false, "columnPriority": 0 },
                  { "name": "Notes", "category": "String", "nullable": false, "columnPriority": {{(category == "ColumnPriority" ? "1" : "9")}}, "displayName": "Old", "fieldGroup": "Schedule", "description": "Old" }
                ] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Count(d => d.GetMessage().Contains("Notes", StringComparison.Ordinal)
                            && d.GetMessage().Contains(category, StringComparison.OrdinalIgnoreCase))
            .ShouldBe(1, $"AC7 caps category '{category}' to ≤1 diagnostic per declaration.");
        // Ensure baseline parameter referenced (avoid IDE0060 false positive for the dev later).
        oldAttr.ShouldNotBeNull();
    }

    [Fact(Skip = SkipReason)]
    public void MultipleCategoriesChangedOnSameDeclaration_OneDiagnosticPerCategory() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                [System.ComponentModel.DataAnnotations.Display(Name="NEW")]
                [System.ComponentModel.Description("NEW")]
                [ColumnPriority(7)]
                [ProjectionFieldGroup("NEW")]
                public string Notes { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Notes", "category": "String", "nullable": false,
                                 "columnPriority": 1, "displayName": "OLD", "fieldGroup": "OLD", "description": "OLD" }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic[] notesDrifts = [.. diagnostics.Where(d => d.GetMessage().Contains("Notes", StringComparison.Ordinal))];
        notesDrifts.Length.ShouldBeGreaterThanOrEqualTo(4, "Each impact category should produce its own diagnostic.");
        notesDrifts.Length.ShouldBeLessThanOrEqualTo(8, "AC7 keeps the cap to ≤1 per category — total bounded by category count.");
    }

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()], additionalTexts: [baselineText]);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
