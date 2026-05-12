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

    [Theory()]
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

    [Fact()]
    public void MultipleCategoriesChangedOnSameDeclaration_ExactlyOneDiagnosticPerCategory() {
        // Story 9-1 review CB-21: AC7 says "at most one diagnostic per declaration per impact
        // category". With four categories changing, the count must equal exactly four — the
        // earlier `Between(4, 8)` bound let a per-category dedup regression slip through.
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
        notesDrifts.Length.ShouldBe(4, "AC7 — exactly one diagnostic per affected metadata category (Display.Name, Description, ColumnPriority, ProjectionFieldGroup).");
    }

    [Fact()]
    public void RelativeTimeWindowChange_EmitsMetadataDiagnostic() {
        // Story 9-1 review CB-7: AC7 enumerates [RelativeTime] as a renderer-impacting category.
        // Source-side attributes apply to DateTimeOffset properties; baseline records the days
        // window in `relativeTimeWindowDays`.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                [RelativeTime(14)]
                public System.DateTimeOffset OccurredAt { get; set; }
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "OccurredAt", "category": "DateTimeOffset", "nullable": false,
                                 "displayFormat": "RelativeTime", "relativeTimeWindowDays": 7 }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.Id == "HFC1066"
                          && d.GetMessage().Contains("RelativeTime", StringComparison.Ordinal)
                          && d.GetMessage().Contains("OccurredAt", StringComparison.Ordinal))
            .ShouldBeTrue("AC7 — relative-time window change must emit one HFC1066 metadata-drift diagnostic.");
    }

    [Fact()]
    public void RequiresPolicyChange_EmitsMetadataDiagnostic() {
        // Story 9-1 review CB-7: AC7 enumerates [RequiresPolicy] as a renderer-impacting category
        // at the contract level. Changing the policy name must surface a metadata-drift diagnostic.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Command]
            [RequiresPolicy("Orders.Manage")]
            public partial class CancelCommand {
                public string MessageId { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "command", "type": "TestDomain.CancelCommand", "boundedContext": "Orders",
                "requiresPolicy": "Orders.Read",
                "properties": [{ "name": "MessageId", "category": "String", "nullable": false, "derivable": false }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.Id == "HFC1066"
                          && d.GetMessage().Contains("RequiresPolicy", StringComparison.Ordinal)
                          && d.GetMessage().Contains("CancelCommand", StringComparison.Ordinal))
            .ShouldBeTrue("AC7 — RequiresPolicy change must emit one HFC1066 metadata-drift diagnostic.");
    }

    [Fact()]
    public void ProjectionBadgeMappingChange_EmitsSingleMetadataDiagnostic() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public OrderStatus Status { get; set; }
            }

            public enum OrderStatus {
                [ProjectionBadge(BadgeSlot.Danger)] New,
                [ProjectionBadge(BadgeSlot.Success)] Done
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Status", "category": "Enum", "nullable": false,
                  "badges": [
                    { "enumMember": "Done", "slot": "Danger" },
                    { "enumMember": "New", "slot": "Success" }
                  ] }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Count(d => d.Id == "HFC1066"
                            && d.GetMessage().Contains("ProjectionBadge", StringComparison.Ordinal)
                            && d.GetMessage().Contains("Status", StringComparison.Ordinal))
            .ShouldBe(1, "AC8 — ProjectionBadge drift must emit exactly one HFC1066 per member/category.");
    }

    [Theory()]
    [InlineData("ProjectionRole", "[ProjectionRole(ProjectionRole.ActionQueue)]", "\"role\": \"StatusOverview\",")]
    [InlineData("ProjectionEmptyStateCta", "[ProjectionEmptyStateCta(\"CreateOrderCommand\")]", "\"emptyStateCtaCommandTypeName\": \"OldCommand\",")]
    public void ProjectionContractMetadataChange_EmitsSingleMetadataDiagnostic(string kind, string attribute, string baselineMetadata) {
        string source = $$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            {{attribute}}
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;
        string baseline = $$"""
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                {{baselineMetadata}}
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Count(d => d.Id == "HFC1066"
                            && d.GetMessage().Contains(kind, StringComparison.Ordinal)
                            && d.GetMessage().Contains("OrderProjection", StringComparison.Ordinal))
            .ShouldBe(1, $"AC8 — {kind} drift must emit exactly one HFC1066 per declaration/category.");
    }

    [Fact()]
    public void CurrencyDisplayFormatChange_EmitsSingleMetadataDiagnostic() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                [Currency]
                public decimal Amount { get; set; }
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Amount", "category": "Decimal", "nullable": false, "displayFormat": "Default" }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Count(d => d.Id == "HFC1066"
                            && d.GetMessage().Contains("DisplayFormat", StringComparison.Ordinal)
                            && d.GetMessage().Contains("Amount", StringComparison.Ordinal))
            .ShouldBe(1, "AC8 — currency/display-format drift must emit exactly one HFC1066 per member/category.");
    }

    [Theory()]
    [InlineData("Destructive", "[Destructive]", "\"destructive\": false,")]
    [InlineData("Icon", "[Icon(\"Regular.Size16.Delete\")]", "\"icon\": \"Regular.Size16.Play\",")]
    public void CommandContractMetadataChange_EmitsSingleMetadataDiagnostic(string kind, string attribute, string baselineMetadata) {
        string source = $$"""
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Command]
            {{attribute}}
            public partial class CancelOrderCommand {
                public string MessageId { get; set; } = string.Empty;
                public string Reason { get; set; } = string.Empty;
            }
            """;
        string baseline = $$"""
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "command", "type": "TestDomain.CancelOrderCommand", "boundedContext": "Orders",
                {{baselineMetadata}}
                "properties": [
                  { "name": "MessageId", "category": "String", "nullable": false, "derivable": false },
                  { "name": "Reason", "category": "String", "nullable": false, "derivable": false }
                ] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Count(d => d.Id == "HFC1066"
                            && d.GetMessage().Contains(kind, StringComparison.Ordinal)
                            && d.GetMessage().Contains("CancelOrderCommand", StringComparison.Ordinal))
            .ShouldBe(1, $"AC8 — {kind} command metadata drift must emit exactly one HFC1066 per declaration/category.");
    }

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
