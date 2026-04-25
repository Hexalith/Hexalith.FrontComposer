using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

public sealed class UnsupportedColumnEmissionTests {
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void Transform_UnsupportedProperty_ProducesUnsupportedColumn() {
        RazorModel result = RazorModelTransform.Transform(Model(Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Tag>>")));

        result.Columns.Count.ShouldBe(1);
        result.Columns[0].PropertyName.ShouldBe("Metadata");
        result.Columns[0].Header.ShouldBe("Metadata");
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Unsupported);
    }

    [Fact]
    public void Transform_MixedSupportedAndUnsupportedProperties_PreservesDeclarationOrder() {
        RazorModel result = RazorModelTransform.Transform(Model(
            Prop("Id", "Guid"),
            Prop("Status", "Enum"),
            Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>")));

        result.Columns.Count.ShouldBe(3);
        result.Columns.Select(c => c.PropertyName).ShouldBe(["Id", "Status", "Metadata"]);
    }

    [Fact]
    public void Transform_PrioritySort_UnsupportedColumnWithNullPriority_SinksAfterPrioritizedColumn() {
        RazorModel result = RazorModelTransform.Transform(Model(
            Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>"),
            Prop("Name", "String", priority: 1)));

        result.Columns.Select(c => c.PropertyName).ShouldBe(["Name", "Metadata"]);
        result.Columns[1].Priority.ShouldBeNull();
        result.Columns[1].TypeCategory.ShouldBe(TypeCategory.Unsupported);
    }

    [Fact]
    public void Transform_UnsupportedProperty_FieldGroupFlowsThrough() {
        RazorModel result = RazorModelTransform.Transform(Model(Unsupported(
            "Metadata",
            "System.Collections.Generic.Dictionary<string, string>",
            fieldGroup: "Metadata")));

        result.Columns[0].FieldGroup.ShouldBe("Metadata");
    }

    [Fact]
    public void Transform_UnsupportedProperty_DoesNotSupportFiltering() {
        RazorModel result = RazorModelTransform.Transform(Model(Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>")));

        result.Columns[0].SupportsFilter.ShouldBeFalse();
    }

    private static DomainModel Model(params PropertyModel[] props)
        => new("TestProjection", "TestDomain", "Test", null, null, new EquatableArray<PropertyModel>(props.ToImmutableArray()));

    private static PropertyModel Prop(string name, string typeName, int? priority = null)
        => new(
            name,
            typeName,
            isNullable: false,
            isUnsupported: false,
            displayName: null,
            EmptyBadges,
            columnPriority: priority);

    private static PropertyModel Unsupported(string name, string typeName, string? fieldGroup = null)
        => new(
            name,
            typeName,
            isNullable: false,
            isUnsupported: true,
            displayName: null,
            EmptyBadges,
            unsupportedTypeFullyQualifiedName: typeName,
            fieldGroup: fieldGroup);
}
