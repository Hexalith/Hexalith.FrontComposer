using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

public sealed class DescriptionTransformTests {
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void Transform_PropertyDescription_FlowsToColumnDescription() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Name", "String", description: "Visible to operators")));

        result.Columns[0].Description.ShouldBe("Visible to operators");
    }

    [Fact]
    public void Transform_NullPropertyDescription_FlowsAsNull() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Name", "String")));

        result.Columns[0].Description.ShouldBeNull();
    }

    [Fact]
    public void Transform_UnsupportedPropertyDescription_FlowsToColumnDescription() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop(
            "Metadata",
            "System.Collections.Generic.Dictionary<string, string>",
            isUnsupported: true,
            description: "Raw integration metadata")));

        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Unsupported);
        result.Columns[0].Description.ShouldBe("Raw integration metadata");
    }

    [Fact]
    public void Transform_PrioritySort_KeepsDescriptionWithOriginalProperty() {
        RazorModel result = RazorModelTransform.Transform(Model(
            Prop("Late", "String", priority: 10, description: "Late description"),
            Prop("Early", "String", priority: 1, description: "Early description")));

        result.Columns[0].PropertyName.ShouldBe("Early");
        result.Columns[0].Description.ShouldBe("Early description");
        result.Columns[1].PropertyName.ShouldBe("Late");
        result.Columns[1].Description.ShouldBe("Late description");
    }

    [Fact]
    public void Transform_DescriptionParticipatesInColumnEquality() {
        ColumnModel described = RazorModelTransform.Transform(Model(Prop("Name", "String", description: "Description A"))).Columns[0];
        ColumnModel other = RazorModelTransform.Transform(Model(Prop("Name", "String", description: "Description B"))).Columns[0];

        described.Equals(other).ShouldBeFalse();
    }

    [Fact]
    public void Transform_EmptyStateCtaCommandName_FlowsToRazorModel() {
        DomainModel domain = new(
            "TestProjection",
            "TestDomain",
            "Test",
            null,
            null,
            new EquatableArray<PropertyModel>(ImmutableArray.Create(Prop("Name", "String"))),
            emptyStateCtaCommandTypeName: "CreateOrderCommand");

        RazorModel result = RazorModelTransform.Transform(domain);

        result.EmptyStateCtaCommandName.ShouldBe("CreateOrderCommand");
    }

    private static DomainModel Model(params PropertyModel[] props)
        => new("TestProjection", "TestDomain", "Test", null, null, new EquatableArray<PropertyModel>(props.ToImmutableArray()));

    private static PropertyModel Prop(
        string name,
        string typeName,
        bool isUnsupported = false,
        int? priority = null,
        string? description = null)
        => new(
            name,
            typeName,
            isNullable: false,
            isUnsupported,
            displayName: null,
            EmptyBadges,
            unsupportedTypeFullyQualifiedName: isUnsupported ? typeName : null,
            columnPriority: priority,
            description: description);
}
