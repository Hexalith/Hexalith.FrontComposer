using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

public sealed class Level1FormatEmitterTests {
    private static readonly EquatableArray<BadgeMappingEntry> EmptyBadges = new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void Emit_CurrencyColumn_UsesCurrentCultureAndNumericClass() {
        string source = RazorEmitter.Emit(Model(Col("Amount", "Amount", TypeCategory.Numeric, "C", displayFormat: FieldDisplayFormat.Currency)));

        source.ShouldContain(".ToString(\"C\", CultureInfo.CurrentCulture)");
        source.ShouldContain("\"Class\", \"fc-col-numeric\"");
        source.ShouldNotContain("SortBy\", (Expression<Func<OrderProjection, string?>>)");
    }

    [Fact]
    public void Emit_NullableCurrencyColumn_PreservesDashFallback() {
        string source = RazorEmitter.Emit(Model(Col("Amount", "Amount", TypeCategory.Numeric, "C", isNullable: true, displayFormat: FieldDisplayFormat.Currency)));

        source.ShouldContain("x.Amount.HasValue ? x.Amount.Value.ToString(\"C\", CultureInfo.CurrentCulture) : \"\\u2014\"");
    }

    [Fact]
    public void Emit_RelativeTimeColumn_CapturesNowOnceAndUsesHelper() {
        string source = RazorEmitter.Emit(Model(Col("LastChanged", "Last changed", TypeCategory.DateTime, "RelativeTime", displayFormat: FieldDisplayFormat.RelativeTime)));

        source.ShouldContain("var relativeNow = TimeProvider.GetUtcNow();");
        source.ShouldContain("FormatRelativeTime(x.LastChanged, relativeNow, 7)");
    }

    [Fact]
    public void Emit_RelativeTimeNullableColumn_PreservesDashFallback() {
        string source = RazorEmitter.Emit(Model(Col("LastChanged", "Last changed", TypeCategory.DateTime, "RelativeTime", isNullable: true, displayFormat: FieldDisplayFormat.RelativeTime)));

        source.ShouldContain("x.LastChanged.HasValue ? FormatRelativeTime(x.LastChanged.Value, relativeNow, 7) : \"\\u2014\"");
    }

    private static RazorModel Model(params ColumnModel[] columns)
        => new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(columns.ToImmutableArray()));

    private static ColumnModel Col(
        string name,
        string header,
        TypeCategory category,
        string? formatHint,
        bool isNullable = false,
        FieldDisplayFormat displayFormat = FieldDisplayFormat.Default)
        => new(name, header, category, formatHint, isNullable, EmptyBadges, displayFormat: displayFormat);
}
