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

    // ──────────────────────────────────────────────────────────────────────────
    //   Story 6-1 review F19 / T1 — story-owned regression for the pre-existing
    //   ColumnPriority semantics (lower-first, unannotated → int.MaxValue,
    //   declaration-order tiebreak). Locks the Story 4-4 contract through the
    //   Story 6-1 ParseDisplayFormat changes so a future refactor that drops
    //   priority sorting on Level 1 columns cannot regress silently.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Transform_UnannotatedPriority_SortsAfterAnnotatedPriorities() {
        RazorModel model = RazorModelTransform.Transform(Model(
            Prop("UnannotatedA", "String"),
            Prop("Annotated", "String", priority: 100),
            Prop("UnannotatedB", "String")));

        // Lower priority is earlier; unannotated → int.MaxValue, so they sort after.
        model.Columns.Select(c => c.PropertyName).ShouldBe(["Annotated", "UnannotatedA", "UnannotatedB"]);
    }

    [Fact]
    public void Transform_PriorityExtremes_MinValueSortsFirstMaxValueLast() {
        RazorModel model = RazorModelTransform.Transform(Model(
            Prop("Mid", "String", priority: 0),
            Prop("Pinned", "String", priority: int.MinValue),
            Prop("LastExplicit", "String", priority: int.MaxValue - 1)));

        model.Columns.Select(c => c.PropertyName).ShouldBe(["Pinned", "Mid", "LastExplicit"]);
    }

    [Fact]
    public void Transform_EqualPriority_PreservesDeclarationOrder() {
        RazorModel model = RazorModelTransform.Transform(Model(
            Prop("First", "String", priority: 1),
            Prop("Second", "String", priority: 1),
            Prop("Third", "String", priority: 1)));

        model.Columns.Select(c => c.PropertyName).ShouldBe(["First", "Second", "Third"]);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //   Story 6-1 review F24 / T1 / T10 — story-owned regression for the
    //   Display.Name / [Description] / Display.Description precedence chain
    //   that Level 1 promises to preserve.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Transform_DisplayNameOverridesHumanizedPropertyName() {
        RazorModel model = RazorModelTransform.Transform(Model(
            Prop("CustomerName", "String", displayName: "Recipient")));

        model.Columns[0].Header.ShouldBe("Recipient");
    }

    [Fact]
    public void Transform_PropertyName_HumanizesWhenDisplayNameAbsent() {
        RazorModel model = RazorModelTransform.Transform(Model(
            Prop("CustomerName", "String")));

        // Default header should NOT be the raw camelCase name; it must be humanized.
        model.Columns[0].Header.ShouldBe("Customer Name");
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
