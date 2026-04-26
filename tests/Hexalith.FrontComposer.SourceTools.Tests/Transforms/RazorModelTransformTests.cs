using System.Collections.Generic;
using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

public class RazorModelTransformTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void Transform_BooleanProperty_MapsToBoolean() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("IsActive", "Boolean")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Boolean);
        result.Columns[0].FormatHint.ShouldBe("Yes/No");
    }

    [Fact]
    public void Transform_CollectionProperty_MapsToCollection() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Items", "Collection")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Collection);
        result.Columns[0].FormatHint.ShouldBe("Count");
    }

    [Fact]
    public void Transform_DateOnlyProperty_MapsToDateTime() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("BirthDate", "DateOnly")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.DateTime);
        result.Columns[0].FormatHint.ShouldBe("d");
    }

    [Fact]
    public void Transform_DateTimeOffsetProperty_MapsToDateTime() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Timestamp", "DateTimeOffset")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.DateTime);
        result.Columns[0].FormatHint.ShouldBe("d");
    }

    [Fact]
    public void Transform_DateTimeProperty_MapsToDateTime() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("CreatedAt", "DateTime")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.DateTime);
        result.Columns[0].FormatHint.ShouldBe("d");
    }

    [Fact]
    public void Transform_DecimalProperty_MapsToNumeric() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Price", "Decimal")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Numeric);
        result.Columns[0].FormatHint.ShouldBe("N2");
    }

    [Fact]
    public void Transform_DisplayNameTakesPriority_OverHumanized() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("OrderDate", "DateTime", displayName: "Date Ordered")));
        result.Columns[0].Header.ShouldBe("Date Ordered");
    }

    [Fact]
    public void Transform_DoubleProperty_MapsToNumeric() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Rate", "Double")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Numeric);
        result.Columns[0].FormatHint.ShouldBe("N2");
    }

    [Fact]
    public void Transform_EnumColumn_HasHumanize30FormatHint() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Status", "Enum")));
        result.Columns[0].FormatHint.ShouldBe("Humanize:30");
    }

    [Fact]
    public void Transform_EnumProperty_MapsToEnum() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Status", "Enum")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Enum);
        result.Columns[0].FormatHint.ShouldBe("Humanize:30");
    }

    [Fact]
    public void Transform_GuidProperty_MapsToText() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Id", "Guid")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Text);
        result.Columns[0].FormatHint.ShouldBe("Truncate:8");
    }

    // --- Label resolution ---
    [Fact]
    public void Transform_HumanizedCamelCase_WhenNoDisplayName() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("OrderDate", "DateTime")));
        result.Columns[0].Header.ShouldBe("Order Date");
    }

    [Fact]
    public void Transform_Int32Property_MapsToNumeric() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Count", "Int32")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Numeric);
        result.Columns[0].FormatHint.ShouldBe("N0");
    }

    [Fact]
    public void Transform_Int64Property_MapsToNumeric() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("BigCount", "Int64")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Numeric);
        result.Columns[0].FormatHint.ShouldBe("N0");
    }

    [Fact]
    public void Transform_LabelResolution_AllThreeSteps() {
        // Step 1: DisplayName wins
        PropertyModel p1 = Prop("XMLParser", "String", displayName: "Custom Label");
        RazorModel r1 = RazorModelTransform.Transform(Model(p1));
        r1.Columns[0].Header.ShouldBe("Custom Label");

        // Step 2: Humanized wins over raw
        PropertyModel p2 = Prop("XMLParser", "String");
        RazorModel r2 = RazorModelTransform.Transform(Model(p2));
        r2.Columns[0].Header.ShouldBe("XML Parser");

        // Step 3: Raw fallback (single char that humanizes to itself)
        PropertyModel p3 = Prop("X", "String");
        RazorModel r3 = RazorModelTransform.Transform(Model(p3));
        r3.Columns[0].Header.ShouldBe("X");
    }

    [Fact]
    public void Transform_NullableCollection_SetsIsNullableAndCollection() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Items", "Collection", isNullable: true)));
        result.Columns[0].IsNullable.ShouldBeTrue();
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Collection);
    }

    [Fact]
    public void Transform_NullableInt_SetsIsNullableTrue() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Count", "Int32", isNullable: true)));
        result.Columns[0].IsNullable.ShouldBeTrue();
    }

    // --- Nullable ---
    [Fact]
    public void Transform_NullableString_SetsIsNullableTrue() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Name", "String", isNullable: true)));
        result.Columns[0].IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Transform_PreservesMetadata() {
        var model = new DomainModel("OrderProjection", "MyApp.Orders", "Orders", null, "StatusOverview",
            new EquatableArray<PropertyModel>(ImmutableArray.Create(Prop("Name", "String"))));
        RazorModel result = RazorModelTransform.Transform(model);

        result.TypeName.ShouldBe("OrderProjection");
        result.Namespace.ShouldBe("MyApp.Orders");
        result.BoundedContext.ShouldBe("Orders");
    }

    [Fact]
    public void Transform_ResolvesProjectionEntityLabels_FromDisplayMetadata() {
        var model = new DomainModel(
            "OrderProjection",
            "MyApp.Orders",
            "Orders",
            null,
            "DetailRecord",
            new EquatableArray<PropertyModel>(ImmutableArray.Create(Prop("Name", "String"))),
            displayName: "Order",
            displayGroupName: "Orders");

        RazorModel result = RazorModelTransform.Transform(model);

        result.EntityLabel.ShouldBe("Order");
        result.EntityPluralLabel.ShouldBe("Orders");
    }

    [Fact]
    public void Transform_PluralizesDisplayName_WhenDisplayGroupNameIsAbsent() {
        var model = new DomainModel(
            "OrderProjection",
            "MyApp.Orders",
            "Orders",
            null,
            "Default",
            new EquatableArray<PropertyModel>(ImmutableArray.Create(Prop("Name", "String"))),
            displayName: "Order");

        RazorModel result = RazorModelTransform.Transform(model);

        result.EntityLabel.ShouldBe("Order");
        result.EntityPluralLabel.ShouldBe("orders");
    }

    [Fact]
    public void Transform_ActionQueueWithoutStatusDriver_EmitsHFC1022WithSourceLocation() {
        DomainModel model = new(
            "QueueProjection",
            "TestDomain",
            "Orders",
            null,
            "ActionQueue",
            new EquatableArray<PropertyModel>(ImmutableArray.Create(Prop("Name", "String"))),
            projectionRoleWhenState: "Pending",
            sourceFilePath: "QueueProjection.cs",
            sourceLine: 12,
            sourceColumn: 4);

        List<DiagnosticInfo> diagnostics = [];

        _ = RazorModelTransform.Transform(model, diagnostics);

        diagnostics.Count.ShouldBe(1);
        diagnostics[0].Id.ShouldBe("HFC1022");
        diagnostics[0].FilePath.ShouldBe("QueueProjection.cs");
        diagnostics[0].Line.ShouldBe(12);
        diagnostics[0].Column.ShouldBe(4);
        diagnostics[0].Message.ShouldContain("requires an enum status property");
    }

    [Fact]
    public void Transform_TimelineWithoutDateDriver_EmitsHFC1022WithSourceLocation() {
        DomainModel model = new(
            "TimelineProjection",
            "TestDomain",
            "Orders",
            null,
            "Timeline",
            new EquatableArray<PropertyModel>(ImmutableArray.Create(Prop("Name", "String"))),
            sourceFilePath: "TimelineProjection.cs",
            sourceLine: 22,
            sourceColumn: 6);

        List<DiagnosticInfo> diagnostics = [];

        _ = RazorModelTransform.Transform(model, diagnostics);

        diagnostics.Count.ShouldBe(1);
        diagnostics[0].Id.ShouldBe("HFC1022");
        diagnostics[0].FilePath.ShouldBe("TimelineProjection.cs");
        diagnostics[0].Line.ShouldBe(22);
        diagnostics[0].Column.ShouldBe(6);
        diagnostics[0].Message.ShouldContain("timeline ordering falls back to declaration order");
    }

    [Fact]
    public void Transform_PreservesEnumMemberDeclarationOrderForEmitters() {
        PropertyModel statusProperty = new(
            "Status",
            "Enum",
            isNullable: false,
            isUnsupported: false,
            displayName: null,
            badgeMappings: _emptyBadges,
            enumFullyQualifiedName: "TestDomain.OrderStatus",
            unsupportedTypeFullyQualifiedName: null,
            enumMemberNames: new EquatableArray<string>(ImmutableArray.Create("Submitted", "Pending")));

        RazorModel result = RazorModelTransform.Transform(Model(statusProperty));

        result.Columns[0].EnumMemberNames.Count.ShouldBe(2);
        result.Columns[0].EnumMemberNames[0].ShouldBe("Submitted");
        result.Columns[0].EnumMemberNames[1].ShouldBe("Pending");
    }

    [Fact]
    public void Transform_RawPropertyName_WhenSimpleName() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Name", "String")));
        result.Columns[0].Header.ShouldBe("Name");
    }

    [Fact]
    public void Transform_SingleProperty_MapsToNumeric() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Score", "Single")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Numeric);
        result.Columns[0].FormatHint.ShouldBe("N2");
    }

    [Fact]
    public void Transform_StringProperty_MapsToText() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("Name", "String")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.Text);
        result.Columns[0].FormatHint.ShouldBeNull();
    }

    // --- Type inference tests (14 types) ---
    [Fact]
    public void Transform_TimeOnlyProperty_MapsToDateTime() {
        RazorModel result = RazorModelTransform.Transform(Model(Prop("StartTime", "TimeOnly")));
        result.Columns[0].TypeCategory.ShouldBe(TypeCategory.DateTime);
        result.Columns[0].FormatHint.ShouldBe("t");
    }

    [Fact]
    public void Transform_UnsupportedPropertiesBecomePlaceholderColumns() {
        PropertyModel[] props =
        [
            Prop("Name", "String"),
            Prop("Count", "Int32"),
            Prop("IsActive", "Boolean"),
            Prop("CreatedAt", "DateTime"),
            Prop("Status", "Enum"),
            Prop("ByteArray", "byte[]", isUnsupported: true),
            Prop("Dict", "Dictionary<string,int>", isUnsupported: true),
        ];
        RazorModel result = RazorModelTransform.Transform(Model(props));
        result.Columns.Count.ShouldBe(7);
        result.Columns[5].TypeCategory.ShouldBe(TypeCategory.Unsupported);
        result.Columns[6].TypeCategory.ShouldBe(TypeCategory.Unsupported);
    }

    private static DomainModel Model(params PropertyModel[] props)
        => new("TestProjection", "TestDomain", "Test", null, null, new EquatableArray<PropertyModel>(props.ToImmutableArray()));

    private static PropertyModel Prop(string name, string typeName, bool isNullable = false, bool isUnsupported = false, string? displayName = null)
                                                                                                            => new(name, typeName, isNullable, isUnsupported, displayName, _emptyBadges);

    // --- Unsupported properties skipped ---
    // --- Enum format hint ---
    // --- Label resolution fallback chain ---
    // --- No BoundedContext uses namespace ---
}
