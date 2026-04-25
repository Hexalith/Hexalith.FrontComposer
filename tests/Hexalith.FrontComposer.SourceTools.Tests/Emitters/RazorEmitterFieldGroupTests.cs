using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-5 T6 / AC5 — emitted detail accordions for [ProjectionFieldGroup].
/// </summary>
public sealed class RazorEmitterFieldGroupTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void HasSecondaryFieldGroupAnnotation_ReturnsFalse_WhenOnlyPrimaryColumnsAreGrouped() {
        RazorModel model = Model(
            ProjectionRenderStrategy.DetailRecord,
            Col("Id", group: "Primary"),
            Col("Name"),
            Col("Status"),
            Col("CreatedAt"),
            Col("Total"),
            Col("Owner"));

        ProjectionRoleBodyEmitter.HasSecondaryFieldGroupAnnotation(model).ShouldBeFalse();
    }

    [Fact]
    public void HasSecondaryFieldGroupAnnotation_ReturnsTrue_WhenSecondaryColumnIsGrouped() {
        RazorModel model = Model(
            ProjectionRenderStrategy.DetailRecord,
            Col("Id"),
            Col("Name"),
            Col("Status"),
            Col("CreatedAt"),
            Col("Total"),
            Col("Owner"),
            Col("ShippingStreet", group: "Shipping"));

        ProjectionRoleBodyEmitter.HasSecondaryFieldGroupAnnotation(model).ShouldBeTrue();
    }

    [Fact]
    public void DetailRecord_WithNoSecondaryFieldGroups_KeepsLegacySingleAccordionShape() {
        string src = RazorEmitter.Emit(Model(
            ProjectionRenderStrategy.DetailRecord,
            Col("Id"),
            Col("Name"),
            Col("Status"),
            Col("CreatedAt"),
            Col("Total"),
            Col("Owner"),
            Col("Notes"),
            Col("Reference")));

        src.ShouldContain("FluentAccordion");
        src.ShouldNotContain("FieldGroupCatchAllTitle");
        src.ShouldNotContain("\"Heading\", \"Shipping\"");
    }

    [Fact]
    public void DetailRecord_WithSecondaryFieldGroups_EmitsGroupedHeadingsAndCatchAllLast() {
        string src = RazorEmitter.Emit(Model(
            ProjectionRenderStrategy.DetailRecord,
            Col("Id"),
            Col("Name"),
            Col("Status"),
            Col("CreatedAt"),
            Col("Total"),
            Col("Owner"),
            Col("UngroupedBefore"),
            Col("ShippingStreet", group: "Shipping"),
            Col("BillingReference", group: "Billing"),
            Col("ShippingCity", group: "Shipping"),
            Col("UngroupedAfter")));

        src.ShouldContain("FluentAccordionItem");
        src.ShouldContain("\"Heading\", \"Shipping\"");
        src.ShouldContain("\"Heading\", \"Billing\"");
        src.ShouldContain("FieldGroupCatchAllTitle");

        int shippingIdx = src.IndexOf("\"Heading\", \"Shipping\"", StringComparison.Ordinal);
        int billingIdx = src.IndexOf("\"Heading\", \"Billing\"", StringComparison.Ordinal);
        int catchAllIdx = src.IndexOf("FieldGroupCatchAllTitle", StringComparison.Ordinal);

        shippingIdx.ShouldBeGreaterThanOrEqualTo(0);
        billingIdx.ShouldBeGreaterThan(shippingIdx);
        catchAllIdx.ShouldBeGreaterThan(billingIdx);

        src.IndexOf("ShippingStreet", StringComparison.Ordinal).ShouldBeLessThan(
            src.IndexOf("ShippingCity", StringComparison.Ordinal));
    }

    private static RazorModel Model(ProjectionRenderStrategy strategy, params ColumnModel[] columns)
        => new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(columns)),
            strategy);

    private static ColumnModel Col(string name, string? group = null)
        => new(name, name, TypeCategory.Text, null, false, _emptyBadges, fieldGroup: group);
}
