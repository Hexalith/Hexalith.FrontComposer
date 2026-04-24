using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-2 T5.1 (AC1, AC3, D5, D8, D10, D14) — targeted unit coverage for
/// <see cref="ColumnEmitter.EmitColumn"/> badge-aware branches. The full end-to-end
/// emission paths are covered by the <c>RoleSpecificProjectionApprovalTests</c>
/// approval baselines; this file provides fast, focused assertions for the Story 1-5
/// regression gate and for the nullable / partial / Flags edge cases.
/// </summary>
public class RazorEmitterBadgeColumnTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);
    private static readonly EquatableArray<string> _emptyEnumMembers =
        new(ImmutableArray<string>.Empty);

    /// <summary>
    /// Story 4-2 T5.3 + AC3 — regression gate. An enum column without any
    /// <c>[ProjectionBadge]</c> mapping must emit the byte-for-byte Story 1-5 text path
    /// with no badge ChildContent.
    /// </summary>
    [Fact]
    public void ZeroMappings_EmitsPlainTextPath_AndNoBadgeChildContent() {
        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", false, _emptyBadges, _emptyEnumMembers))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("Truncate(HumanizeEnumLabel(x.Status.ToString()), 30)");
        source.ShouldNotContain("FcStatusBadge");
        source.ShouldNotContain("ChildContent\", (RenderFragment<OrderProjection>");
    }

    /// <summary>
    /// Story 4-2 AC3 — partial coverage. A mapped enum member emits a
    /// <c>FcStatusBadge</c> switch arm; unmapped members fall through to the humanized
    /// text default arm. The <c>Property</c> lambda stays intact for sort / filter.
    /// </summary>
    [Fact]
    public void PartialMappings_EmitsSwitchWithBadgeArmsPlusTextDefault() {
        EquatableArray<BadgeMappingEntry> badges = new(ImmutableArray.Create(
            new BadgeMappingEntry("Pending", "Warning"),
            new BadgeMappingEntry("Approved", "Success")));

        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", false, badges, _emptyEnumMembers))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("(Expression<Func<OrderProjection, string?>>)(x => Truncate(HumanizeEnumLabel(x.Status.ToString()), 30))");
        source.ShouldContain("case \"Pending\":");
        source.ShouldContain("BadgeSlot.Warning");
        source.ShouldContain("case \"Approved\":");
        source.ShouldContain("BadgeSlot.Success");
        source.ShouldContain("default:");
        source.ShouldContain("rb.AddContent(10, _label);");
    }

    /// <summary>
    /// Story 4-2 D8 / AC1 — nullable enum values render the em-dash fallback before any
    /// badge switch. Preserves the Story 1-5 "null is data-absence" convention.
    /// </summary>
    [Fact]
    public void NullableEnum_WithBadgeMappings_EmitsNullCheckBeforeSwitch() {
        EquatableArray<BadgeMappingEntry> badges = new(ImmutableArray.Create(
            new BadgeMappingEntry("Pending", "Warning")));

        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", true, badges, _emptyEnumMembers))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("if (!item.Status.HasValue)");
        source.ShouldContain("rb.AddContent(0, \"\\u2014\");");
        source.ShouldContain("var _memberName = item.Status.Value.ToString();");
        source.ShouldContain("case \"Pending\":");
    }

    /// <summary>
    /// Story 4-2 D14 / AC1 — <c>HumanizeEnumLabel</c> is the label source for badges too,
    /// not a resource lookup. Guards against the "localise enum labels via resource keys"
    /// slip where a well-meaning refactor routes badge text through
    /// <see cref="Microsoft.Extensions.Localization.IStringLocalizer"/>.
    /// </summary>
    [Fact]
    public void BadgeLabelUsesHumanizeEnumLabel_NotResourceLookup() {
        EquatableArray<BadgeMappingEntry> badges = new(ImmutableArray.Create(
            new BadgeMappingEntry("Active", "Success")));

        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", false, badges, _emptyEnumMembers))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("var _label = HumanizeEnumLabel(_memberName);");
        source.ShouldNotContain("IStringLocalizer");
    }

    /// <summary>
    /// Story 4-2 AC5 — generated badge emission always threads the column header into the
    /// <c>ColumnHeader</c> parameter so adopter-side screen readers announce the contextual
    /// state, not a bare state name.
    /// </summary>
    [Fact]
    public void BadgeEmission_SuppliesColumnHeaderForAriaLabelContext() {
        EquatableArray<BadgeMappingEntry> badges = new(ImmutableArray.Create(
            new BadgeMappingEntry("Active", "Success")));

        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Status", "Order status", TypeCategory.Enum, "Humanize:30", false, badges, _emptyEnumMembers))));

        string source = RazorEmitter.Emit(model);

        source.ShouldContain("rb.AddAttribute(3, \"ColumnHeader\", \"Order status\");");
    }

    /// <summary>
    /// Story 4-2 — output must remain valid C# at every branch. Parses a badge-annotated
    /// emission through Roslyn and asserts no syntactic diagnostics.
    /// </summary>
    [Fact]
    public void BadgeEmission_ParsesAsValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        EquatableArray<BadgeMappingEntry> badges = new(ImmutableArray.Create(
            new BadgeMappingEntry("Pending", "Warning"),
            new BadgeMappingEntry("Approved", "Success"),
            new BadgeMappingEntry("Rejected", "Danger")));

        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Status", "Status", TypeCategory.Enum, "Humanize:30", true, badges, _emptyEnumMembers))));

        string source = RazorEmitter.Emit(model);
        Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
        tree.GetDiagnostics(ct).ShouldBeEmpty("Emitted badge code should parse without syntax errors");
    }
}
