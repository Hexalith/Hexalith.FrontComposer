using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

public sealed class Level1FormatTransformTests {
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void Transform_RelativeTimeFormat_CarriesUiAgnosticColumnMetadata() {
        RazorModel model = RazorModelTransform.Transform(Model(Prop("LastChanged", "DateTime", FieldDisplayFormat.RelativeTime)));

        model.Columns[0].DisplayFormat.ShouldBe(FieldDisplayFormat.RelativeTime);
        model.Columns[0].FormatHint.ShouldBe("RelativeTime");
        model.Columns[0].RelativeTimeWindowDays.ShouldBe(7);
    }

    [Fact]
    public void Transform_CurrencyFormat_CarriesColumnMetadataAndKeepsNumericCategory() {
        RazorModel model = RazorModelTransform.Transform(Model(Prop("Amount", "Decimal", FieldDisplayFormat.Currency)));

        model.Columns[0].DisplayFormat.ShouldBe(FieldDisplayFormat.Currency);
        model.Columns[0].TypeCategory.ShouldBe(TypeCategory.Numeric);
        model.Columns[0].FormatHint.ShouldBe("C");
    }

    [Fact]
    public void Transform_DisplayNameStillWins_WhenFormatAnnotationIsPresent() {
        RazorModel model = RazorModelTransform.Transform(Model(
            Prop("LastChanged", "DateTime", FieldDisplayFormat.RelativeTime, displayName: "Last changed")));

        model.Columns[0].Header.ShouldBe("Last changed");
    }

    [Fact]
    public void Transform_FormatOverride_DoesNotChangePrioritySortOrder() {
        RazorModel model = RazorModelTransform.Transform(Model(
            Prop("Unannotated", "String"),
            Prop("Amount", "Decimal", FieldDisplayFormat.Currency, priority: 2),
            Prop("LastChanged", "DateTime", FieldDisplayFormat.RelativeTime, priority: 1)));

        model.Columns.Select(c => c.PropertyName).ShouldBe(["LastChanged", "Amount", "Unannotated"]);
    }

    private static DomainModel Model(params PropertyModel[] props)
        => new("TestProjection", "TestDomain", "Test", null, null, new EquatableArray<PropertyModel>(props.ToImmutableArray()));

    private static PropertyModel Prop(
        string name,
        string typeName,
        FieldDisplayFormat displayFormat = FieldDisplayFormat.Default,
        string? displayName = null,
        int? priority = null)
        => new(
            name,
            typeName,
            isNullable: false,
            isUnsupported: false,
            displayName,
            EmptyBadges,
            columnPriority: priority,
            displayFormat: displayFormat);
}
