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

    // ──────────────────────────────────────────────────────────────────────────
    //   Story 6-1 review F23 / T7 — cross-role formatter agreement. The same
    //   Currency / RelativeTime format expression must reach every render role
    //   that consumes the annotated field. Standard DataGrid covered by the
    //   tests above; the tests below cover ActionQueue, StatusOverview,
    //   DetailRecord, and Timeline — and pin their shared use of the
    //   FormatValueExpression / ColumnEmitter formatter.
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProjectionRenderStrategy.Default)]
    [InlineData(ProjectionRenderStrategy.ActionQueue)]
    [InlineData(ProjectionRenderStrategy.StatusOverview)]
    [InlineData(ProjectionRenderStrategy.DetailRecord)]
    [InlineData(ProjectionRenderStrategy.Dashboard)]
    public void EveryGridRole_EmitsCurrencyFormatExpression_OnCurrencyColumn(ProjectionRenderStrategy strategy) {
        // Story 6-1 review F23 / T7 — DataGrid + DetailRecord + StatusOverview consume the
        // ColumnEmitter / FormatValueExpression numeric path which routes Currency through
        // `.ToString("C", CultureInfo.CurrentCulture)`. Dashboard delegates to Default.
        string source = RazorEmitter.Emit(Model(
            strategy,
            Col("Status", "Status", TypeCategory.Enum, null),
            Col("Amount", "Amount", TypeCategory.Numeric, "C", displayFormat: FieldDisplayFormat.Currency)));

        source.ShouldContain(".ToString(\"C\", CultureInfo.CurrentCulture)");
    }

    [Theory]
    [InlineData(ProjectionRenderStrategy.Default)]
    [InlineData(ProjectionRenderStrategy.ActionQueue)]
    [InlineData(ProjectionRenderStrategy.StatusOverview)]
    [InlineData(ProjectionRenderStrategy.DetailRecord)]
    [InlineData(ProjectionRenderStrategy.Timeline)]
    [InlineData(ProjectionRenderStrategy.Dashboard)]
    public void EveryRole_EmitsRelativeTimeHelperCall_OnRelativeTimeColumn(ProjectionRenderStrategy strategy) {
        // Story 6-1 review F23 / T7 — every role renders the RelativeTime DateTime column.
        // Timeline uses it as the chronological-order column; the rest render it as a column /
        // detail field. All paths share the same FormatRelativeTime helper.
        string source = RazorEmitter.Emit(Model(
            strategy,
            Col("Status", "Status", TypeCategory.Enum, null),
            Col("LastChanged", "Last changed", TypeCategory.DateTime, "RelativeTime", displayFormat: FieldDisplayFormat.RelativeTime)));

        source.ShouldContain("FormatRelativeTime(");
        source.ShouldContain("relativeNow");
    }

    [Fact]
    public void TimelineRole_OmitsCurrencyColumn_FromDefaultBody_ButTemplateRendererKeepsFrameworkFormat() {
        // Story 6-1 review F23 / T7 — documented intentional fallback. Timeline renders a
        // chronological order column + label + status enum; pure numeric fields (including
        // [Currency]) are not part of the Timeline row vocabulary. The column is dropped at
        // the role-body emit stage rather than rendered with a different formatter, so no
        // Currency `.ToString("C", ...)` call should appear in the emitted source.
        string source = RazorEmitter.Emit(Model(
            ProjectionRenderStrategy.Timeline,
            Col("OccurredAt", "Occurred", TypeCategory.DateTime, "d"),
            Col("Title", "Title", TypeCategory.Text, null),
            Col("Status", "Status", TypeCategory.Enum, null),
            Col("Amount", "Amount", TypeCategory.Numeric, "C", displayFormat: FieldDisplayFormat.Currency)));

        string defaultBodySource = source[source.IndexOf("global::Microsoft.AspNetCore.Components.RenderFragment defaultBody", StringComparison.Ordinal)..];
        defaultBodySource.ShouldNotContain(".ToString(\"C\", CultureInfo.CurrentCulture)");
        source.ShouldContain(".ToString(\"C\", CultureInfo.CurrentCulture)");
    }

    private static RazorModel Model(params ColumnModel[] columns)
        => Model(ProjectionRenderStrategy.Default, columns);

    private static RazorModel Model(ProjectionRenderStrategy strategy, params ColumnModel[] columns)
        => new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(columns.ToImmutableArray()),
            strategy: strategy);

    private static ColumnModel Col(
        string name,
        string header,
        TypeCategory category,
        string? formatHint,
        bool isNullable = false,
        FieldDisplayFormat displayFormat = FieldDisplayFormat.Default)
        => new(name, header, category, formatHint, isNullable, EmptyBadges, displayFormat: displayFormat);
}
