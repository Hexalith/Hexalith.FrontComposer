using System.Collections.Immutable;
using System.IO;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-1 T3.10 / ADR-052 — strategy dispatch invariants for <see cref="RazorEmitter.Emit"/>.
/// Focused coverage (not exhaustive per role-by-role snapshot — those are separate approvals
/// gated by CI — this suite asserts the dispatch wiring is correct and the shared shells are
/// always present regardless of role).
/// </summary>
public class RazorEmitterStrategyDispatchTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);
    private static readonly EquatableArray<string> _defaultStatusOrder =
        new(ImmutableArray.Create("Pending", "Submitted", "Approved"));

    private static readonly EquatableArray<string> _noWhenStates =
        new(ImmutableArray<string>.Empty);

    private static RazorModel BuildModel(
        ProjectionRenderStrategy strategy,
        EquatableArray<string>? whenStates = null) =>
        new RazorModel(
            typeName: "OrderProjection",
            @namespace: "TestDomain",
            boundedContext: "Orders",
            columns: new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text),
                Col("Status", "Status", TypeCategory.Enum, "Humanize:30", badges: new EquatableArray<BadgeMappingEntry>(
                    ImmutableArray.Create(new BadgeMappingEntry("Pending", "Warning"))), enumMemberNames: _defaultStatusOrder),
                Col("Count", "Count", TypeCategory.Numeric, "N0"),
                Col("CreatedAt", "Created At", TypeCategory.DateTime, "d"))),
            strategy: strategy,
            whenStates: whenStates ?? _noWhenStates);

    private static ColumnModel Col(
        string name,
        string header,
        TypeCategory category,
        string? formatHint = null,
        bool isNullable = false,
        EquatableArray<BadgeMappingEntry>? badges = null,
        EquatableArray<string> enumMemberNames = default) =>
        new ColumnModel(name, header, category, formatHint, isNullable, badges ?? _emptyBadges, enumMemberNames);

    [Theory]
    [InlineData(ProjectionRenderStrategy.Default)]
    [InlineData(ProjectionRenderStrategy.ActionQueue)]
    [InlineData(ProjectionRenderStrategy.StatusOverview)]
    [InlineData(ProjectionRenderStrategy.DetailRecord)]
    [InlineData(ProjectionRenderStrategy.Timeline)]
    [InlineData(ProjectionRenderStrategy.Dashboard)]
    public void EveryStrategyEmitsSubtitleAndShells(ProjectionRenderStrategy strategy) {
        string output = RazorEmitter.Emit(BuildModel(strategy));

        output.ShouldContain("FcProjectionSubtitle");
        output.ShouldContain("FcProjectionLoadingSkeleton");
        output.ShouldContain("FcProjectionEmptyPlaceholder");
    }

    [Fact]
    public void DefaultStrategyEmitsStandardDataGridBody() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Default));

        output.ShouldContain("FluentDataGrid<OrderProjection>");
        output.ShouldContain("state.Items.AsQueryable()");
    }

    [Fact]
    public void ActionQueueStrategyWithWhenStateEmitsFilter() {
        string output = RazorEmitter.Emit(BuildModel(
            ProjectionRenderStrategy.ActionQueue,
            whenStates: new EquatableArray<string>(ImmutableArray.Create("Pending", "Submitted"))));

        output.ShouldContain(".Where(x => ");
        output.ShouldContain("\"Pending\"");
        output.ShouldContain("\"Submitted\"");
    }

    [Fact]
    public void ActionQueueStrategyWithoutWhenStateOmitsFilter() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.ActionQueue));

        output.ShouldContain("_cachedActionQueueItems = state.Items.ToList();");
        output.ShouldContain("(_cachedActionQueueItems ?? state.Items).AsQueryable()");
        output.ShouldNotContain(".Where(x => x.Status.ToString() ==");
    }

    [Fact]
    public void StatusOverviewStrategyEmitsGroupingAndNavigation() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.StatusOverview));

        output.ShouldContain("GroupBy(x => x.Status)");
        output.ShouldContain("OrderProjectionStatusOverviewRow");
        output.ShouldContain("FcProjectionRoutes.StatusFilter");
        output.ShouldContain("Navigation.NavigateTo");
        output.ShouldNotContain("FluentDataGrid<dynamic>");
        output.ShouldNotContain("Expression<Func<dynamic");
    }

    [Fact]
    public void StatusOverviewStrategyUsesEnumDeclarationOrderTieBreaks() {
        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col(
                    "Status",
                    "Status",
                    TypeCategory.Enum,
                    "Humanize:30",
                    badges: new EquatableArray<BadgeMappingEntry>(ImmutableArray.Create(new BadgeMappingEntry("Submitted", "Warning"))),
                    enumMemberNames: new EquatableArray<string>(ImmutableArray.Create("Submitted", "Pending"))),
                Col("CreatedAt", "Created At", TypeCategory.DateTime, "d"))),
            ProjectionRenderStrategy.StatusOverview,
            _noWhenStates);

        string output = RazorEmitter.Emit(model);

        output.ShouldContain("\"Submitted\" => 0L");
        output.ShouldContain("\"Pending\" => 1L");
        output.ShouldNotContain("Convert.ToInt64");
    }

    [Fact]
    public void DetailRecordStrategyEmitsFluentCard() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.DetailRecord));

        output.ShouldContain("FluentCard");
        output.ShouldContain("state.Items[0]");
        // 4 supported columns ≤ 6 primary cap → NO accordion
        output.ShouldNotContain("FluentAccordion");
    }

    [Fact]
    public void DetailRecordWithMoreThanSixPropertiesEmitsAccordion() {
        EquatableArray<ColumnModel> columns = new(ImmutableArray.Create(
            Col("A", "A", TypeCategory.Text),
            Col("B", "B", TypeCategory.Text),
            Col("C", "C", TypeCategory.Text),
            Col("D", "D", TypeCategory.Text),
            Col("E", "E", TypeCategory.Text),
            Col("F", "F", TypeCategory.Text),
            Col("G", "G", TypeCategory.Text),
            Col("H", "H", TypeCategory.Text)));

        RazorModel model = new(
            "WideProjection", "TestDomain", "X",
            columns,
            ProjectionRenderStrategy.DetailRecord,
            _noWhenStates);

        string output = RazorEmitter.Emit(model);

        output.ShouldContain("FluentCard");
        output.ShouldContain("FluentAccordion");
    }

    [Fact]
    public void TimelineStrategyEmitsOrderingByDateTime() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Timeline));

        output.ShouldContain("OrderByDescending(x => x.CreatedAt)");
        output.ShouldContain("FluentStack");
        output.ShouldContain("Orientation.Vertical");
    }

    [Fact]
    public void TimelineStrategyUsesColumnFormatAndGuidLikeLabelFormatting() {
        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text, "Truncate:8"),
                Col("OccurredAt", "Occurred At", TypeCategory.DateTime, "t"),
                Col(
                    "Status",
                    "Status",
                    TypeCategory.Enum,
                    "Humanize:30",
                    badges: new EquatableArray<BadgeMappingEntry>(ImmutableArray.Create(new BadgeMappingEntry("Pending", "Warning"))),
                    enumMemberNames: _defaultStatusOrder))),
            ProjectionRenderStrategy.Timeline,
            _noWhenStates);

        string output = RazorEmitter.Emit(model);

        output.ShouldContain("item.OccurredAt.ToString(\"t\", CultureInfo.CurrentCulture)");
        output.ShouldContain("item.Id.ToString(\"N\")");
        output.ShouldNotContain("ToString(\"g\", CultureInfo.CurrentCulture)");
    }

    [Fact]
    public void TimelineStrategyPrefersDescriptiveLabelOverIdLikeText() {
        RazorModel model = new(
            "AuditProjection",
            "TestDomain",
            "Audit",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                Col("Id", "Id", TypeCategory.Text),
                Col("Actor", "Actor", TypeCategory.Text),
                Col("Description", "Description", TypeCategory.Text),
                Col("OccurredAt", "Occurred At", TypeCategory.DateTime, "g"),
                Col(
                    "Severity",
                    "Severity",
                    TypeCategory.Enum,
                    "Humanize:30",
                    badges: new EquatableArray<BadgeMappingEntry>(ImmutableArray.Create(new BadgeMappingEntry("Pending", "Warning"))),
                    enumMemberNames: _defaultStatusOrder))),
            ProjectionRenderStrategy.Timeline,
            _noWhenStates);

        string output = RazorEmitter.Emit(model);

        output.ShouldContain("b.AddContent(rowSeq++, item.Description);");
        output.ShouldNotContain("b.AddContent(rowSeq++, item.Id);");
    }

    [Fact]
    public void DashboardStrategyDelegatesToDefaultBody() {
        string dashboard = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Dashboard));
        string @default = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Default));

        // Dashboard body should match Default body (same FluentDataGrid shape).
        dashboard.ShouldContain("FluentDataGrid<OrderProjection>");
        // Role attribute differs between Dashboard and Default invocations; but the body shape
        // (FluentDataGrid + PropertyColumns) is the same.
        int dashboardColumnCount = CountOccurrences(dashboard, "PropertyColumn<OrderProjection, string?>");
        int defaultColumnCount = CountOccurrences(@default, "PropertyColumn<OrderProjection, string?>");
        dashboardColumnCount.ShouldBe(defaultColumnCount);
    }

    [Fact]
    public void DefaultStrategyEmitsEmptyShellWithNullRoleCast() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Default));

        output.ShouldContain("(Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole?)null");
    }

    [Fact]
    public void NonDefaultStrategyEmitsEmptyShellWithTypedRoleLiteral() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.ActionQueue));

        output.ShouldContain("Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole.ActionQueue");
    }

    [Fact]
    public void EmptyShellEmitsCtaCommandName() {
        // Story 4-6 review fix (D17): SecondaryText is no longer emitted by the generator —
        // FcProjectionEmptyPlaceholder resolves the convention key
        // ({ProjectionFqn}_EmptyStateSecondaryText) internally so adopters can override per
        // projection without re-emit. Generator emits CtaCommandName only.
        RazorModel model = new(
            "OrderProjection",
            "TestDomain",
            "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(Col("Id", "Id", TypeCategory.Text))),
            ProjectionRenderStrategy.Default,
            _noWhenStates,
            emptyStateCtaCommandName: "CreateOrderCommand");

        string output = RazorEmitter.Emit(model);

        output.ShouldContain("CtaCommandName\", \"CreateOrderCommand\"");
        output.ShouldNotContain("ResolveEmptyStateSecondaryText");
        output.ShouldNotContain("SecondaryText\",");
    }

    [Fact]
    public void EmptyShellEmitsNullCtaCommandNameWhenNoCommandIsDeclared() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Default));

        output.ShouldContain("CtaCommandName\", (string?)null");
        output.ShouldNotContain("ResolveEmptyStateSecondaryText");
    }

    [Fact]
    public void LoadingShellEmitsRoleAwareLayout() {
        string detailCard = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.DetailRecord));
        string timeline = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Timeline));
        string dataGrid = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Default));

        detailCard.ShouldContain("SkeletonLayout.Card");
        timeline.ShouldContain("SkeletonLayout.Timeline");
        dataGrid.ShouldContain("SkeletonLayout.DataGrid");
    }

    [Fact]
    public void ActionQueueStrategyEmitsRowContextCascade() {
        // Story 4-1 G12 / D14 / AC1 — ActionQueue body emits a trailing
        // TemplateColumn that constructs `_rowContext` per iteration and wraps
        // an empty render fragment in `<CascadingValue Value="_rowContext" IsFixed="true">`.
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.ActionQueue));

        output.ShouldContain("TemplateColumn<OrderProjection>");
        output.ShouldContain("ProjectionContext");
        output.ShouldContain("CascadingValue");
        output.ShouldContain("IsFixed", customMessage: "ActionQueue must mark the cascade IsFixed=true to prevent re-render storm under Story 4.4 virtualization (D14)");
        output.ShouldContain("fc-row-context-actions");
    }

    [Fact]
    public void ActionQueueStrategyEmitsMemoizationAndDefaultDescendingSort() {
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.ActionQueue));

        output.ShouldContain("ReferenceEquals(_cachedActionQueueSource, state.Items)");
        output.ShouldContain("private List<OrderProjection>? _cachedActionQueueItems;");
        output.ShouldContain("_cachedActionQueueItems");
        output.ShouldContain(".ToList();");
        output.ShouldContain("IsDefaultSortColumn");
        output.ShouldContain("InitialSortDirection");
        output.ShouldContain("DataGridSortDirection.Descending");
        output.ShouldContain("SortBy");
    }

    [Fact]
    public void DefaultStrategyDoesNotEmitRowContextCascade() {
        // Story 4-1 AC5 + G12 — Default does NOT emit the ActionQueue-specific
        // ProjectionContext cascade (`fc-row-context-actions`). The trailing
        // `TemplateColumn<OrderProjection>` introduced by Story 4-5 (expand-in-row
        // chevron, class `fc-row-action-column`) is the only TemplateColumn on Default.
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Default));

        output.ShouldNotContain("fc-row-context-actions");
    }

    [Fact]
    public void ActionQueueRowContextResolvesAggregateIdProperty() {
        // Story 4-1 D14 — when the projection declares an "Id" property the cascade
        // wires aggregateId via `item.Id`. The fixture above has a non-nullable string Id, so
        // the emitted context construction must reference it.
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.ActionQueue));

        output.ShouldContain("aggregateId: item.Id");
    }

    [Fact]
    public void SubtitleAndLoadingShellPassEntityLabelsAndLoadingState() {
        string defaultOutput = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.Default));
        string statusOutput = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.StatusOverview));

        defaultOutput.ShouldContain("EntityLabel");
        defaultOutput.ShouldContain("EntityPluralLabel");
        defaultOutput.ShouldContain("IsLoading");
        defaultOutput.ShouldContain("\"orders\"");
        statusOutput.ShouldContain("DistinctStatusCount");
    }

    [Fact]
    public void SubtitleIsEmittedOutsideRowCascade() {
        // Story 4-1 T3.10 / FMA round 3 — the subtitle invocation sits ABOVE the
        // FluentDataGrid emission, never inside the per-row TemplateColumn. If a
        // refactor placed the subtitle inside the row cascade, the subtitle would
        // re-render per row and trigger catastrophic subscription fanout (D21).
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.ActionQueue));

        int subtitleIdx = output.IndexOf("FcProjectionSubtitle", System.StringComparison.Ordinal);
        int gridIdx = output.IndexOf("FluentDataGrid<OrderProjection>", System.StringComparison.Ordinal);
        subtitleIdx.ShouldBeGreaterThanOrEqualTo(0);
        gridIdx.ShouldBeGreaterThan(subtitleIdx, customMessage: "FcProjectionSubtitle must be emitted before FluentDataGrid (outside any per-row cascade)");
    }

    [Fact]
    public void RowContextIsConstructedAsLoopLocalNotClassField() {
        // Story 4-1 T3.10 / round 4 / Winston — code-shape companion to AC1d.
        // Regex-scan the emitter output for class-scoped `_rowContext` declarations
        // (private/protected/internal field shapes); ALL `_rowContext` mentions
        // must be inside an iteration construct (RenderFragment<T> lambda body),
        // never as a class field.
        string output = RazorEmitter.Emit(BuildModel(ProjectionRenderStrategy.ActionQueue));

        // Class-scoped `_rowContext` declarations would look like
        //   private ProjectionContext _rowContext
        //   protected ProjectionContext _rowContext
        //   internal ProjectionContext _rowContext
        // None of those forms must appear in emitted output.
        output.ShouldNotContain(
            "private global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext _rowContext",
            customMessage: "_rowContext must be a per-iteration local in the row loop, not a class field — see D14 + AC1d for rationale.");
        output.ShouldNotContain(
            "protected global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext _rowContext",
            customMessage: "_rowContext must be a per-iteration local in the row loop, not a class field — see D14 + AC1d for rationale.");
        output.ShouldNotContain(
            "internal global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext _rowContext",
            customMessage: "_rowContext must be a per-iteration local in the row loop, not a class field — see D14 + AC1d for rationale.");

        // Positive assertion: `_rowContext` MUST be inside a per-iteration RenderFragment<T>
        // lambda body. The construction line `var _rowContext = new` proves loop-local scope.
        output.ShouldContain("var _rowContext = new global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext(");
    }

    [Fact]
    public void EmitsExhaustiveDispatchWithThrowingDefault() {
        // The switch-expression approach inside DispatchBody has a throwing default arm
        // (ADR-052). We can't execute that path at runtime without bypassing the enum, but
        // we can assert the generated source contains the InvalidOperationException guard
        // as defensive documentation.
        string emitterSource = File.ReadAllText(ResolveEmitterSourcePath());

        emitterSource.ShouldContain("throw new InvalidOperationException(");
        emitterSource.ShouldContain("Unhandled ProjectionRenderStrategy");
    }

    private static string ResolveEmitterSourcePath() {
        DirectoryInfo? cursor = new(AppContext.BaseDirectory);
        while (cursor is not null && !File.Exists(Path.Combine(cursor.FullName, "Hexalith.FrontComposer.sln"))) {
            cursor = cursor.Parent;
        }

        string repoRoot = cursor?.FullName
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        return Path.Combine(
            repoRoot,
            "src",
            "Hexalith.FrontComposer.SourceTools",
            "Emitters",
            "RazorEmitter.cs");
    }

    private static int CountOccurrences(string source, string needle) {
        int count = 0;
        int pos = 0;
        while ((pos = source.IndexOf(needle, pos, System.StringComparison.Ordinal)) != -1) {
            count++;
            pos += needle.Length;
        }

        return count;
    }
}
