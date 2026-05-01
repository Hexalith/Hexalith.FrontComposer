using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class FcFieldPlaceholderColumnEmissionTests {
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);
    private static readonly EquatableArray<string> NoWhenStates = new(ImmutableArray<string>.Empty);

    [Fact]
    public void UnsupportedColumn_EmitsTemplateColumnWithFieldPlaceholder() {
        string output = RazorEmitter.Emit(Model(Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>")));

        output.ShouldContain("TemplateColumn<OrderProjection>");
        output.ShouldContain("FcFieldPlaceholder");
        output.ShouldNotContain("PropertyColumn<OrderProjection, string?>");
    }

    [Fact]
    public void UnsupportedColumn_EmitsFieldNameTypeNameAndDevModeParameters() {
        string output = RazorEmitter.Emit(Model(Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>")));

        output.ShouldContain("FieldName\", \"Metadata\"");
        output.ShouldContain("TypeName\", \"System.Collections.Generic.Dictionary<string, string>\"");
        output.ShouldContain("IsDevMode");
    }

    [Fact]
    public void UnsupportedColumn_StillEmitsResolvedHeaderTitle() {
        string output = RazorEmitter.Emit(Model(Unsupported(
            "Metadata",
            "System.Collections.Generic.Dictionary<string, string>",
            header: "Integration metadata")));

        output.ShouldContain("Title\", \"Integration metadata \" + FcShellLocalizer[\"UnsupportedColumnHeaderSuffix\"].Value");
    }

    [Fact]
    public void AllUnsupportedColumns_DoNotCollapseToEmptyStateOnly() {
        string output = RazorEmitter.Emit(Model(
            Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>"),
            Unsupported("Tags", "System.ReadOnlyMemory<byte>")));

        // Story 6-3: unsupported grid cells now enter the Level 3 slot host and share the
        // template default renderer, so placeholder source appears once per default renderer path.
        // GD-P3 (review of Story 6-3 Group D, 2026-05-01): EmitDetailField gates on
        // HasProjectionSlot, emitting EmitDetailFieldValue in BOTH the slot-path renderDefault
        // and the else branch (only one runs at runtime). Total per unsupported column:
        // 1× grid TemplateColumn cell + 2× detail panel (slot renderDefault + non-slot else) = 3.
        // 2 unsupported columns × 3 = 6.
        CountOccurrences(output, "FcFieldPlaceholder").ShouldBe(6);
        output.ShouldContain("FluentDataGrid<OrderProjection>");
    }

    [Fact]
    public void UnsupportedColumn_ParticipatesInColumnPrioritizerDescriptorList() {
        ImmutableArray<ColumnModel>.Builder columns = ImmutableArray.CreateBuilder<ColumnModel>();
        for (int i = 0; i < 15; i++) {
            columns.Add(Col("Name" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), "Name " + i.ToString(System.Globalization.CultureInfo.InvariantCulture), TypeCategory.Text));
        }

        columns.Add(Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>"));

        string output = RazorEmitter.Emit(Model(new EquatableArray<ColumnModel>(columns.ToImmutable())));

        output.ShouldContain("new global::Hexalith.FrontComposer.Shell.Components.DataGrid.ColumnDescriptor(\"Metadata\", \"Metadata\", (int?)null)");
    }

    [Fact]
    public void UnsupportedColumn_DoesNotEmitFilterCell() {
        string output = RazorEmitter.Emit(Model(Unsupported("Metadata", "System.Collections.Generic.Dictionary<string, string>")));

        output.ShouldNotContain("FcColumnFilterCell");
        output.ShouldNotContain("SortBy");
    }

    private static RazorModel Model(params ColumnModel[] columns)
        => Model(new EquatableArray<ColumnModel>(columns.ToImmutableArray()));

    private static RazorModel Model(EquatableArray<ColumnModel> columns)
        => new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            columns,
            ProjectionRenderStrategy.Default,
            NoWhenStates);

    private static ColumnModel Unsupported(string name, string typeName, string? header = null)
        => new(
            name,
            header ?? name,
            TypeCategory.Unsupported,
            formatHint: null,
            isNullable: false,
            EmptyBadges,
            unsupportedTypeFullyQualifiedName: typeName);

    private static ColumnModel Col(string name, string header, TypeCategory category)
        => new(name, header, category, formatHint: null, isNullable: false, EmptyBadges);

    private static int CountOccurrences(string source, string needle) {
        int count = 0;
        int pos = 0;
        while ((pos = source.IndexOf(needle, pos, StringComparison.Ordinal)) != -1) {
            count++;
            pos += needle.Length;
        }

        return count;
    }
}
