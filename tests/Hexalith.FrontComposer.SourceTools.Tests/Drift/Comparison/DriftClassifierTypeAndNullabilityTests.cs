using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison;

/// <summary>
/// AC5 / T3 — type/category change and nullability change classification. Both projection and
/// command property surfaces are covered. Activation asserts: (a) Warning severity by default;
/// (b) message identifies BOTH the prior category and the current category; (c) message names
/// the affected rendering surface (form input, DataGrid column, filter, badge/format, MCP descriptor).
/// </summary>
public sealed class DriftClassifierTypeAndNullabilityTests {
    private const string SkipReason = "RED-PHASE: T3 — type/nullability classifier not yet introduced.";

    [Theory()]
    [InlineData("string", "int", "form input")]
    [InlineData("int", "string", "DataGrid")]
    [InlineData("System.DateTime", "System.DateTimeOffset", "format")]
    [InlineData("decimal", "double", "currency")]
    public void TypeCategoryChange_OnProjection_EmitsWarningWithBothCategories(string baselineType, string currentType, string expectedSurface) {
        string baseline = $$"""
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Amount", "category": "{{CategoryFor(baselineType)}}", "nullable": false }] }] }
            """;

        string source = $$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public {{currentType}} Amount { get; set; }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic? typeDrift = diagnostics.FirstOrDefault(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                                                             && d.GetMessage().Contains("Amount", StringComparison.Ordinal)
                                                             && d.Severity == DiagnosticSeverity.Warning);
        typeDrift.ShouldNotBeNull("AC5 type change must emit a Warning by default.");
        typeDrift!.GetMessage().ShouldContain(CategoryFor(baselineType), Case.Insensitive);
        typeDrift.GetMessage().ShouldContain(CategoryFor(currentType), Case.Insensitive);
        typeDrift.GetMessage().ShouldContain(expectedSurface, Case.Insensitive);
    }

    [Fact()]
    public void NullabilityChange_NonNullableToNullable_EmitsWarning() {
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Reference", "category": "String", "nullable": false }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string? Reference { get; set; }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains("nullable", StringComparison.OrdinalIgnoreCase)
                          && d.GetMessage().Contains("Reference", StringComparison.Ordinal)
                          && d.Severity == DiagnosticSeverity.Warning)
            .ShouldBeTrue("AC5 nullability change must emit a Warning naming the property.");
    }

    [Fact()]
    public void NullabilityChange_NullableToNonNullable_EmitsWarning_WithBreakingHint() {
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Reference", "category": "String", "nullable": true }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Reference { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic? nullabilityDrift = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("Reference", StringComparison.Ordinal)
                                                                    && d.GetMessage().Contains("nullable", StringComparison.OrdinalIgnoreCase));
        nullabilityDrift.ShouldNotBeNull();
        // Story 9-1 review CB-25: anchor with word boundaries so a regression that drops the
        // breaking-hint token but leaves an incidental substring (e.g. "required" inside a
        // doc-link path or "tightly-coupled" prose) does not pass silently.
        nullabilityDrift!.GetMessage().ShouldMatch(@"\b(required|breaking|tightened)\b");
    }

    [Theory()]
    // Story 9-1 review CB-24: extend type-change coverage beyond primitive↔primitive. The
    // boundary cases where the production category mapping crosses a kind-of-thing line
    // (scalar→collection, value→reference, generic-arg) must surface a deterministic
    // structural drift diagnostic; otherwise a regression that misclassifies (or silently
    // accepts) cross-kind transitions slips through.
    [InlineData("string",                       "System.Collections.Generic.IReadOnlyList<string>", "Tags",      "String")]
    [InlineData("int",                          "string",                                            "Reference", "Int32")]
    [InlineData("System.DateTimeOffset",        "string",                                            "OccurredAt","DateTimeOffset")]
    public void TypeCategoryChange_AcrossKinds_EmitsStructuralDrift(string baselineClr, string currentClr, string memberName, string baselineCategory) {
        ArgumentNullException.ThrowIfNull(currentClr);
        string baseline = $$"""
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "{{memberName}}", "category": "{{baselineCategory}}", "nullable": false }] }] }
            """;
        string source = $$"""
            using System.Collections.Generic;
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public {{currentClr}} {{memberName}} { get; set; }{{(currentClr == "string" ? " = string.Empty;" : currentClr.Contains("IReadOnlyList") ? " = [];" : "")}}
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.Id == "HFC1065"
                          && d.GetMessage().Contains(memberName, StringComparison.Ordinal)
                          && d.Severity == DiagnosticSeverity.Warning)
            .ShouldBeTrue($"AC5 cross-kind boundary — {baselineClr} → {currentClr} on {memberName} must emit a structural-drift Warning.");
    }

    [Fact()]
    public void TypeCategoryChange_OnCommandProperty_EmitsWarning_WithFormInputHint() {
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "command", "type": "TestDomain.ConfirmCommand", "boundedContext": "Orders",
                "properties": [{ "name": "Quantity", "category": "Int32", "nullable": false, "derivable": false }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Command]
            public partial class ConfirmCommand {
                public string MessageId { get; set; } = string.Empty;
                public string Quantity { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains("Quantity", StringComparison.Ordinal)
                          && d.GetMessage().Contains("form", StringComparison.OrdinalIgnoreCase)
                          && d.Severity == DiagnosticSeverity.Warning)
            .ShouldBeTrue("AC5 — command type change must call out form input rendering risk.");
    }

    [Fact()]
    public void CommandPropertyAdded_EmitsStructuralDrift_WithFormInputHint() {
        // Story 9-1 review CB-8 (AC3 + T7 matrix): command field add/remove must be classified
        // as structural drift on the command surface — Chunk B previously covered only command
        // type-change. Add-side coverage closes the matrix.
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "command", "type": "TestDomain.ConfirmCommand", "boundedContext": "Orders",
                "properties": [{ "name": "MessageId", "category": "String", "nullable": false, "derivable": false }] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Command]
            public partial class ConfirmCommand {
                public string MessageId { get; set; } = string.Empty;
                public int Quantity { get; set; }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.Id == "HFC1065"
                          && d.GetMessage().Contains("Quantity", StringComparison.Ordinal)
                          && d.GetMessage().Contains("added", StringComparison.OrdinalIgnoreCase)
                          && d.Severity == DiagnosticSeverity.Warning)
            .ShouldBeTrue("AC3 — command field add must emit one structural-drift Warning.");
    }

    [Fact()]
    public void CommandPropertyRemoved_EmitsStructuralDrift_WithFormInputHint() {
        // Story 9-1 review CB-8 (AC3 + T7 matrix): command field remove counterpart.
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "command", "type": "TestDomain.ConfirmCommand", "boundedContext": "Orders",
                "properties": [
                  { "name": "MessageId",  "category": "String", "nullable": false, "derivable": false },
                  { "name": "OldField",   "category": "String", "nullable": false, "derivable": false }
                ] }] }
            """;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Command]
            public partial class ConfirmCommand {
                public string MessageId { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.Id == "HFC1065"
                          && d.GetMessage().Contains("OldField", StringComparison.Ordinal)
                          && (d.GetMessage().Contains("not found", StringComparison.OrdinalIgnoreCase)
                           || d.GetMessage().Contains("removed", StringComparison.OrdinalIgnoreCase))
                          && d.Severity == DiagnosticSeverity.Warning)
            .ShouldBeTrue("AC3 — command field remove must emit one structural-drift Warning naming the removed property.");
    }

    private static string CategoryFor(string clrType) => clrType switch {
        "string" => "String",
        "int" => "Int32",
        "decimal" => "Decimal",
        "double" => "Double",
        "System.DateTime" => "DateTime",
        "System.DateTimeOffset" => "DateTimeOffset",
        _ => "Unknown",
    };

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
}
