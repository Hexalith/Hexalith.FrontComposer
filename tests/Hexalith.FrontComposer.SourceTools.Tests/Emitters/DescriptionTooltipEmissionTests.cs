using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class DescriptionTooltipEmissionTests {
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);
    private static readonly EquatableArray<string> NoWhenStates = new(ImmutableArray<string>.Empty);

    [Fact]
    public void DescriptionColumn_EmitsHeaderTooltip() {
        string output = RazorEmitter.Emit(Model(ProjectionRenderStrategy.Default, Col(
            "OrderId",
            "Order Id",
            TypeCategory.Text,
            description: "Order number for audit trail")));

        output.ShouldContain("HeaderTooltip");
        output.ShouldContain("Order number for audit trail");
    }

    [Fact]
    public void DescriptionlessColumn_EmitsPlainTitleWithoutHeaderTooltip() {
        string output = RazorEmitter.Emit(Model(ProjectionRenderStrategy.Default, Col("OrderId", "Order Id", TypeCategory.Text)));

        output.ShouldContain("Title\", \"Order Id\"");
        output.ShouldNotContain("HeaderTooltip");
    }

    [Fact]
    public void DetailRecord_DescriptionColumn_EmitsCaptionBelowFieldValue() {
        string output = RazorEmitter.Emit(Model(ProjectionRenderStrategy.DetailRecord, Col(
            "OrderId",
            "Order Id",
            TypeCategory.Text,
            description: "Order number for audit trail")));

        output.ShouldContain("Typography.Caption");
        output.ShouldContain("fc-field-description");
        output.ShouldContain("Order number for audit trail");
    }

    [Fact]
    public void DescriptionText_IsEscapedInGeneratedOutput() {
        string output = RazorEmitter.Emit(Model(ProjectionRenderStrategy.DetailRecord, Col(
            "OrderId",
            "Order Id",
            TypeCategory.Text,
            description: "Use <audit> & \"billing\" values")));

        output.ShouldContain("Use <audit> & \\\"billing\\\" values");
        output.ShouldNotContain("AddMarkupContent");
    }

    [Fact]
    public void RazorModelEquality_IncludesDescriptionViaColumns() {
        RazorModel left = Model(ProjectionRenderStrategy.Default, Col("OrderId", "Order Id", TypeCategory.Text, description: "A"));
        RazorModel right = Model(ProjectionRenderStrategy.Default, Col("OrderId", "Order Id", TypeCategory.Text, description: "B"));

        left.Equals(right).ShouldBeFalse();
    }

    [Fact]
    public void DisplayDescription_FlowsToEmitterAfterTransform() {
        var property = new PropertyModel(
            "OrderId",
            "String",
            isNullable: false,
            isUnsupported: false,
            displayName: null,
            EmptyBadges,
            description: "Display description");
        var domain = new DomainModel(
            "OrderProjection",
            "TestDomain",
            "Orders",
            null,
            null,
            new EquatableArray<PropertyModel>(ImmutableArray.Create(property)));

        string output = RazorEmitter.Emit(RazorModelTransform.Transform(domain));

        output.ShouldContain("HeaderTooltip");
        output.ShouldContain("Display description");
    }

    private static RazorModel Model(ProjectionRenderStrategy strategy, params ColumnModel[] columns)
        => new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(columns.ToImmutableArray()),
            strategy,
            NoWhenStates);

    private static ColumnModel Col(
        string name,
        string header,
        TypeCategory category,
        string? description = null)
        => new(name, header, category, formatHint: null, isNullable: false, EmptyBadges, description: description);
}
