using System.Collections.Generic;
using System.Linq;
using System.Text;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

namespace Hexalith.FrontComposer.SourceTools.Emitters;

/// <summary>
/// Story 4-1 T3 / D4 / D5 / ADR-052 — per-role body emitters dispatched from
/// <see cref="RazorEmitter"/> via a switch-expression with throwing default arm.
/// Each <c>Emit{Strategy}Body</c> composes the strategy-specific Data-state
/// rendering inside the view's <c>BuildRenderTree</c>; the shared Loading /
/// Empty shells, subtitle host, and class scaffolding live in
/// <see cref="RazorEmitter"/>.
/// </summary>
/// <remarks>
/// Adding a new strategy requires three coordinated edits: (1) append a member
/// to <see cref="ProjectionRenderStrategy"/>, (2) add a case to the switch in
/// <see cref="RazorEmitter.Emit"/>, (3) add a matching <c>Emit{Name}Body</c>
/// static here. The switch-expression form guarantees the compiler flags any
/// missing dispatch case (ADR-052).
/// </remarks>
public static class ProjectionRoleBodyEmitter {
    internal const string ShellRenderingNamespace = "Hexalith.FrontComposer.Shell.Components.Rendering";
    internal const string ContractsAttributesNamespace = "Hexalith.FrontComposer.Contracts.Attributes";

    /// <summary>
    /// Story 4-1 T3.3 — Default DataGrid body. Mirrors the pre-Story-4-1 Razor
    /// Emitter body: one <see cref="Microsoft.FluentUI.AspNetCore.Components.FluentDataGrid{T}"/>,
    /// one <see cref="Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder"/> per column,
    /// no inline actions, no filter.
    /// </summary>
    public static void EmitDefaultBody(StringBuilder sb, RazorModel model) {
        EmitStandardDataGrid(
            sb,
            model,
            filteredItemsExpression: "state.Items.AsQueryable()",
            emitExpandableRows: true);
    }

    /// <summary>
    /// Story 4-1 T3.4 / D14 / G12 — ActionQueue DataGrid body with inline-action row
    /// cascade + runtime <c>.Where</c> filter over <see cref="RazorModel.WhenStates"/>.
    /// When <c>WhenStates</c> is empty the emission falls through to the default
    /// query-able source. Memoization per D10/C1 uses reference equality on
    /// <c>state.Items</c>. The trailing inline-action <c>TemplateColumn</c>
    /// constructs <c>_rowContext</c> as a per-iteration local (D14 / AC1d / round-4
    /// code-shape invariant) and wraps an empty render fragment in
    /// <c>&lt;CascadingValue Value="_rowContext" IsFixed="true"&gt;</c>; Story 2-2's
    /// inline-command renderer integration will fill that fragment without
    /// touching the emitter.
    /// </summary>
    public static void EmitActionQueueBody(StringBuilder sb, RazorModel model) {
        string filteredSource = ResolveActionQueueFilteredSource(model);
        string? defaultSortPropertyName = RoleBodyHelpers.ResolveActionQueueSortColumn(model)?.PropertyName;

        _ = sb.AppendLine("        if (!ReferenceEquals(_cachedActionQueueSource, state.Items))");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            _cachedActionQueueSource = state.Items;");
        _ = sb.AppendLine("            _cachedActionQueueItems = " + filteredSource + ".ToList();");
        _ = sb.AppendLine("        }");
        _ = sb.AppendLine();
        _ = sb.AppendLine("        var filteredItems = (_cachedActionQueueItems ?? state.Items).AsQueryable();");
        _ = sb.AppendLine();

        EmitStandardDataGrid(
            sb,
            model,
            filteredItemsExpression: "filteredItems",
            emitRowContextActionColumn: true,
            defaultSortPropertyName: defaultSortPropertyName,
            emitExpandableRows: true);
    }

    private static string ResolveActionQueueFilteredSource(RazorModel model) {
        string? predicate = RoleBodyHelpers.ResolveWhenStateFilterPredicate(model);
        return predicate is null
            ? "state.Items"
            : "state.Items.Where(x => " + predicate + ")";
    }

    /// <summary>
    /// Story 4-1 T3.5 — StatusOverview: group <c>state.Items</c> by the first
    /// status-enum property and render a two-column DataGrid of
    /// (Status, Count) rows, sorted by descending count (stable secondary by
    /// enum member declaration order). Row click dispatches navigation through
    /// <see cref="ShellRenderingNamespace"/><c>.FcProjectionRoutes.StatusFilter</c>
    /// so the URL format stays consumer-replaceable (D11 / H2).
    /// </summary>
    public static void EmitStatusOverviewBody(StringBuilder sb, RazorModel model) {
        ColumnModel? statusColumn = RoleBodyHelpers.ResolveStatusEnumColumn(model);
        if (statusColumn is null) {
            // D11 fallback — no status enum property → render the Default body.
            EmitDefaultBody(sb, model);
            return;
        }

        string bcRoute = "/" + (model.BoundedContext ?? model.Namespace.Replace('.', '/'));
        string recordTypeName = model.TypeName + "StatusOverviewRow";
        string statusProperty = statusColumn.PropertyName;

        _ = sb.AppendLine("        var groupedItems = state.Items");
        _ = sb.AppendLine("            .GroupBy(x => x." + statusProperty + ")");
        _ = sb.AppendLine(
            statusColumn.IsNullable
                ? "            .Select(g => new " + recordTypeName + "(g.Key.HasValue ? (Enum?)(object)g.Key.Value : null, g.Key.HasValue ? HumanizeEnumLabel(g.Key.Value.ToString()) : \"\\u2014\", g.Count(), g.Key.HasValue ? GetEnumSortOrder((Enum?)(object)g.Key.Value) : long.MaxValue, g.FirstOrDefault()))"
                : "            .Select(g => new " + recordTypeName + "((Enum?)(object)g.Key, HumanizeEnumLabel(g.Key.ToString()), g.Count(), GetEnumSortOrder((Enum?)(object)g.Key), g.FirstOrDefault()))");
        _ = sb.AppendLine("            .OrderByDescending(g => g.Count)");
        _ = sb.AppendLine("            .ThenBy(g => g.SortOrder)");
        _ = sb.AppendLine("            .ToList();");
        _ = sb.AppendLine();
        _ = sb.AppendLine("        // Story 4-5 — resolve expanded-row state for this StatusOverview render pass.");
        _ = sb.AppendLine("        global::Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandedRowEntry? _expandedEntry = ExpandedRowState.Value.GetEntry(_ephemeralViewKey);");
        _ = sb.AppendLine("        object? _expandedItemKey = _expandedEntry?.ItemKey;");
        _ = sb.AppendLine("        " + recordTypeName + "? _expandedStatusOverviewItem = null;");
        _ = sb.AppendLine("        if (_expandedItemKey is not null)");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            foreach (var __candidate in groupedItems)");
        _ = sb.AppendLine("            {");
        _ = sb.AppendLine("                if (string.Equals(__candidate.StatusLabel, _expandedItemKey.ToString(), StringComparison.Ordinal))");
        _ = sb.AppendLine("                {");
        _ = sb.AppendLine("                    _expandedStatusOverviewItem = __candidate;");
        _ = sb.AppendLine("                    break;");
        _ = sb.AppendLine("                }");
        _ = sb.AppendLine("            }");
        _ = sb.AppendLine("        }");
        _ = sb.AppendLine("        bool _expandedItemHiddenByFilter = _expandedItemKey is not null && _expandedStatusOverviewItem is null;");
        _ = sb.AppendLine();

        _ = sb.AppendLine("        builder.OpenComponent<FluentDataGrid<" + recordTypeName + ">>(seq++);");
        // Story 4-4 T2.1 / D1 / D20 — Virtualize even at low item count keeps emission shape uniform.
        _ = sb.AppendLine("        builder.SetKey(_density);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"Items\", groupedItems.AsQueryable());");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"Virtualize\", true);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"DisplayMode\", Microsoft.FluentUI.AspNetCore.Components.DataGridDisplayMode.Table);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ItemSize\", Hexalith.FrontComposer.Shell.Components.Rendering.DataGridDensityMetrics.ResolveRowHeightPx(_density));");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"OverscanCount\", 3);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ItemKey\", (System.Func<" + recordTypeName + ", object>)(static r => (object)r.StatusLabel));");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"RowClass\", (System.Func<" + recordTypeName + ", string?>)(row => _expandedItemKey is not null && string.Equals(row.StatusLabel, _expandedItemKey.ToString(), StringComparison.Ordinal) ? \"fc-row-expanded\" : null));");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder b) =>");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            int colSeq = 300;");
        _ = sb.AppendLine();
        string statusHeaderLiteral = "\"" + RoleBodyHelpers.EscapeString(statusColumn.Header) + "\"";
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + recordTypeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", " + statusHeaderLiteral + ");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + recordTypeName + ", string?>>)(x => x.StatusLabel));");
        if (statusColumn.BadgeMappings.Count > 0) {
            EmitStatusOverviewBadgeChildContent(sb, statusColumn, recordTypeName, statusHeaderLiteral);
        }
        _ = sb.AppendLine("            b.CloseComponent();");
        _ = sb.AppendLine();
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + recordTypeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"Count\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + recordTypeName + ", string?>>)(x => x.Count.ToString(\"N0\", CultureInfo.CurrentCulture)));");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Class\", \"fc-col-numeric\");");
        _ = sb.AppendLine("            b.CloseComponent();");
        _ = sb.AppendLine();
        EmitStatusOverviewExpandTriggerColumn(sb, recordTypeName);
        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine();
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"OnRowClick\", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<FluentDataGridRow<" + recordTypeName + ">>(this, row =>");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            if (row?.Item?.Status is not Enum status) { return; }");
        _ = sb.AppendLine("            var destination = " + ShellRenderingNamespace + ".FcProjectionRoutes.StatusFilter(\"" + bcRoute + "\", status);");
        _ = sb.AppendLine("            Navigation.NavigateTo(destination);");
        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine("        builder.CloseComponent();");
        _ = sb.AppendLine();
        EmitStatusOverviewDetailPanel(sb, model, recordTypeName);
    }

    /// <summary>
    /// Story 4-1 T3.6 — DetailRecord: render a <see cref="Microsoft.FluentUI.AspNetCore.Components.FluentCard"/>
    /// for the first entity in <c>state.Items</c>; primary fields (≤ 6 in declaration order) render
    /// as <c>FluentLabel</c> / <c>FluentText</c> pairs; secondary fields (positions 7+) render inside
    /// a collapsed <c>FluentAccordion</c>. When total supported properties ≤ 6 the accordion is
    /// omitted to avoid empty-accordion DOM noise (D12).
    /// </summary>
    public static void EmitDetailRecordBody(StringBuilder sb, RazorModel model)
        => EmitDetailRecordBody(
            sb,
            model,
            builderName: "builder",
            itemExpression: "state.Items[0]",
            seqStart: 400,
            groupByFieldGroup: true);

    /// <summary>
    /// Story 4-5 T2.2 / D10 — parameterized helper consumed by both Story 4-1's DetailRecord
    /// role body and Story 4-5's expand-in-row host. The defaults (<c>builderName="builder"</c>,
    /// <c>itemExpression="state.Items[0]"</c>, <c>seqStart=400</c>, <c>groupByFieldGroup=true</c>)
    /// produce byte-identical output to the legacy zero-arg overload when no
    /// <c>[ProjectionFieldGroup]</c> annotations are present — protecting Story 4-1's approval
    /// baselines. The 4-5 expand-in-row host calls with <c>itemExpression="_expandedItem"</c>
    /// and <c>seqStart=800</c> to avoid <c>RenderTreeBuilder</c> sequence-number collisions.
    /// </summary>
    public static void EmitDetailRecordBody(
        StringBuilder sb,
        RazorModel model,
        string builderName,
        string itemExpression,
        int seqStart,
        bool groupByFieldGroup) {
        int primaryCap = 6;
        int supportedCount = model.Columns.Count;
        int primaryCount = Math.Min(primaryCap, supportedCount);
        bool hasSecondary = supportedCount > primaryCap;

        // Detect whether ANY secondary column declares a [ProjectionFieldGroup] annotation.
        // When zero AND groupByFieldGroup is true (the 4-1 default), emit the LEGACY byte-
        // identical single-accordion layout to preserve the 4-1 approval baselines.
        bool anySecondaryGrouped = false;
        if (groupByFieldGroup && hasSecondary) {
            for (int i = primaryCap; i < supportedCount; i++) {
                if (model.Columns[i].FieldGroup is not null) {
                    anySecondaryGrouped = true;
                    break;
                }
            }
        }

        // Bind itemExpression to the "entity" local — the field-emission helpers
        // (EmitDetailField) hardcode "entity" as the item identifier; binding here keeps
        // the helper interface unchanged while letting both call sites pass any item source.
        _ = sb.AppendLine("        var entity = " + itemExpression + ";");
        _ = sb.AppendLine();
        _ = sb.AppendLine("        " + builderName + ".OpenComponent<FluentCard>(seq++);");
        _ = sb.AppendLine("        " + builderName + ".AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder b) =>");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            int fieldSeq = " + seqStart.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";");
        _ = sb.AppendLine();

        for (int i = 0; i < primaryCount; i++) {
            EmitDetailField(sb, model.Columns[i], depth: 3);
        }

        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine("        " + builderName + ".CloseComponent();");

        if (!hasSecondary) {
            return;
        }

        if (!anySecondaryGrouped) {
            // Story 4-1 legacy layout — single FluentAccordionItem, hardcoded sequence numbers
            // 500-503 + secFieldSeq=600. Preserves byte-for-byte parity with 4-1's approval
            // baselines when no [ProjectionFieldGroup] is present (regression gate per D10).
            int legacySecStart = seqStart + 200;
            _ = sb.AppendLine();
            _ = sb.AppendLine("        " + builderName + ".OpenComponent<FluentAccordion>(seq++);");
            _ = sb.AppendLine("        " + builderName + ".AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder b) =>");
            _ = sb.AppendLine("        {");
            _ = sb.AppendLine("            b.OpenComponent<FluentAccordionItem>(" + (seqStart + 100).ToString(System.Globalization.CultureInfo.InvariantCulture) + ");");
            _ = sb.AppendLine("            b.AddAttribute(" + (seqStart + 101).ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"Expanded\", false);");
            _ = sb.AppendLine("            b.AddAttribute(" + (seqStart + 102).ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"HeadingLevel\", 3);");
            _ = sb.AppendLine("            b.AddAttribute(" + (seqStart + 103).ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"ChildContent\", (RenderFragment)((RenderTreeBuilder ib) =>");
            _ = sb.AppendLine("            {");
            _ = sb.AppendLine("                int secFieldSeq = " + legacySecStart.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";");

            for (int i = primaryCap; i < supportedCount; i++) {
                EmitDetailField(sb, model.Columns[i], depth: 4, builderName: "ib", seqName: "secFieldSeq");
            }

            _ = sb.AppendLine("            }));");
            _ = sb.AppendLine("            b.CloseComponent();");
            _ = sb.AppendLine("        }));");
            _ = sb.AppendLine("        " + builderName + ".CloseComponent();");
            return;
        }

        // Story 4-5 D9 / AC5 — grouped emit path. Walk the buckets returned by
        // ResolveFieldGroups; each bucket renders as its own FluentAccordionItem with the
        // group name as Heading (catch-all bucket → FieldGroupCatchAllTitle resource).
        IReadOnlyList<ColumnModel> secondaryColumns = SliceSecondary(model.Columns, primaryCap);
        IReadOnlyList<FieldGroupBucket> buckets = RoleBodyHelpers.ResolveFieldGroups(secondaryColumns);

        int itemSeqCursor = seqStart + 100;
        int innerSeqCursor = seqStart + 200;

        _ = sb.AppendLine();
        _ = sb.AppendLine("        " + builderName + ".OpenComponent<FluentAccordion>(seq++);");
        _ = sb.AppendLine("        " + builderName + ".AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder b) =>");
        _ = sb.AppendLine("        {");

        for (int bi = 0; bi < buckets.Count; bi++) {
            FieldGroupBucket bucket = buckets[bi];
            string headingExpression = bucket.GroupName is null
                ? ColumnEmitter.ShellLocalizerFieldName + "[\"FieldGroupCatchAllTitle\"].Value"
                : "\"" + RoleBodyHelpers.EscapeString(bucket.GroupName) + "\"";

            _ = sb.AppendLine("            b.OpenComponent<FluentAccordionItem>(" + itemSeqCursor++.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");");
            _ = sb.AppendLine("            b.AddAttribute(" + itemSeqCursor++.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"Heading\", " + headingExpression + ");");
            _ = sb.AppendLine("            b.AddAttribute(" + itemSeqCursor++.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"Expanded\", false);");
            _ = sb.AppendLine("            b.AddAttribute(" + itemSeqCursor++.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"HeadingLevel\", 3);");
            _ = sb.AppendLine("            b.AddAttribute(" + itemSeqCursor++.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", \"ChildContent\", (RenderFragment)((RenderTreeBuilder ib) =>");
            _ = sb.AppendLine("            {");
            _ = sb.AppendLine("                int secFieldSeq = " + innerSeqCursor.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";");
            innerSeqCursor += 200;

            for (int j = 0; j < bucket.Columns.Count; j++) {
                EmitDetailField(sb, bucket.Columns[j], depth: 4, builderName: "ib", seqName: "secFieldSeq");
            }

            _ = sb.AppendLine("            }));");
            _ = sb.AppendLine("            b.CloseComponent();");
        }

        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine("        " + builderName + ".CloseComponent();");
    }

    private static IReadOnlyList<ColumnModel> SliceSecondary(EquatableArray<ColumnModel> all, int primaryCap) {
        if (all.Count <= primaryCap) {
            return Array.Empty<ColumnModel>();
        }

        ColumnModel[] tail = new ColumnModel[all.Count - primaryCap];
        for (int i = primaryCap, k = 0; i < all.Count; i++, k++) {
            tail[k] = all[i];
        }

        return tail;
    }

    /// <summary>
    /// Story 4-5 — returns true when the projection has at least one secondary column
    /// (positions 7+) carrying a <c>[ProjectionFieldGroup]</c> annotation. Drives the
    /// conditional <c>FcShellLocalizer</c> injection so non-grouped projections do not
    /// pick up an unused dependency.
    /// </summary>
    public static bool HasSecondaryFieldGroupAnnotation(RazorModel model) {
        const int primaryCap = 6;
        if (model.Columns.Count <= primaryCap) {
            return false;
        }

        for (int i = primaryCap; i < model.Columns.Count; i++) {
            if (model.Columns[i].FieldGroup is not null) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Story 4-1 T3.7 — Timeline: vertical chronological stack ordered by the
    /// first <c>DateTime</c>/<c>DateTimeOffset</c> property descending. If no
    /// DateTime property exists, order falls back to declaration order.
    /// </summary>
    public static void EmitTimelineBody(StringBuilder sb, RazorModel model) {
        ColumnModel? orderColumn = RoleBodyHelpers.ResolveFirstDateTimeColumn(model);
        string? orderProp = orderColumn?.PropertyName;
        ColumnModel? labelColumn = RoleBodyHelpers.ResolveTimelineLabelColumn(model);
        ColumnModel? statusColumn = RoleBodyHelpers.ResolveStatusEnumColumn(model);

        string orderedSource = orderProp is not null
            ? orderColumn!.IsNullable
                ? "state.Items.OrderByDescending(x => x." + orderProp + " ?? default)"
                : "state.Items.OrderByDescending(x => x." + orderProp + ")"
            : "state.Items";

        _ = sb.AppendLine("        var orderedItems = " + orderedSource + ".ToList();");
        _ = sb.AppendLine();
        _ = sb.AppendLine("        builder.OpenComponent<FluentStack>(seq++);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"Orientation\", Orientation.Vertical);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder b) =>");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            int rowSeq = 700;");
        _ = sb.AppendLine("            foreach (var item in orderedItems)");
        _ = sb.AppendLine("            {");
        _ = sb.AppendLine("                b.OpenElement(rowSeq++, \"div\");");
        _ = sb.AppendLine("                b.AddAttribute(rowSeq++, \"class\", \"fc-timeline-row\");");
        if (orderProp is not null) {
            _ = sb.AppendLine("                b.AddContent(rowSeq++, " + FormatValueExpression(orderColumn!, "item") + ");");
            _ = sb.AppendLine("                b.AddMarkupContent(rowSeq++, \" \");");
        }
        if (labelColumn is not null) {
            _ = sb.AppendLine("                b.AddContent(rowSeq++, " + FormatValueExpression(labelColumn!, "item") + ");");
            _ = sb.AppendLine("                b.AddMarkupContent(rowSeq++, \" \");");
        }
        if (statusColumn is not null) {
            // Story 4-2 RF1 — badge-annotated enum rows dispatch through FcStatusBadge; otherwise
            // preserve the Story 4-1 humanized text behaviour.
            if (statusColumn.BadgeMappings.Count > 0) {
                ColumnEmitter.EmitInlineEnumRenderFragment(
                    sb,
                    statusColumn,
                    instanceName: "item",
                    builderName: "b",
                    seqVariable: "rowSeq",
                    indent: "                ");
            }
            else {
                _ = sb.AppendLine("                b.AddContent(rowSeq++, HumanizeEnumLabel(item." + statusColumn.PropertyName + ".ToString()));");
            }
        }
        _ = sb.AppendLine("                b.CloseElement();");
        _ = sb.AppendLine("            }");
        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine("        builder.CloseComponent();");
    }

    /// <summary>
    /// Story 4-1 T3.8 / D16 / AC10 — Dashboard delegates to the Default body in v1
    /// (full Dashboard rendering is deferred to Story 6-3). HFC1023 has already
    /// been emitted at Transform; this method is only the render-side fallback.
    /// </summary>
    public static void EmitDashboardBody(StringBuilder sb, RazorModel model) => EmitDefaultBody(sb, model);

    /// <summary>
    /// Shared DataGrid emission helper — the pre-Story-4-1 body shape carried over
    /// into the Default / ActionQueue / Dashboard strategies. Accepts a custom
    /// <paramref name="filteredItemsExpression"/> so ActionQueue can substitute a
    /// filtered <c>state.Items</c> projection. When
    /// <paramref name="emitRowContextActionColumn"/> is <see langword="true"/> the
    /// helper appends a trailing <c>TemplateColumn</c> that constructs
    /// <c>_rowContext</c> per iteration and wraps an empty render fragment in
    /// <c>&lt;CascadingValue Value="_rowContext" IsFixed="true"&gt;</c> (Story 4-1
    /// D14 / AC1 / G12). Default / Dashboard pass <see langword="false"/> per AC5
    /// "no inline-action column" guidance.
    /// </summary>
    internal static void EmitStandardDataGrid(
        StringBuilder sb,
        RazorModel model,
        string filteredItemsExpression,
        bool emitRowContextActionColumn = false,
        string? defaultSortPropertyName = null,
        bool emitExpandableRows = false) {
        // Story 4-5 T2.1 / D2 / D4 / D6 / D19 / D22 / AC1 / AC2 / AC8 — the host view computes
        // the per-render expansion state OUTSIDE the FluentDataGrid emission so RowClass /
        // OnRowClick / the trailing TemplateColumn can read coherent fields. Three locals are
        // produced: _expandedItemKey (boxed key from ExpandedRowState), _expandedItem (the
        // typed row that matches that key, null when filter has hidden it), and a derived
        // _expandedItemHiddenByFilter bool that drives FcExpandedRowHiddenBanner.
        if (emitExpandableRows) {
            _ = sb.AppendLine("        // Story 4-5 — resolve expanded-row state for this render pass.");
            _ = sb.AppendLine("        global::Hexalith.FrontComposer.Shell.State.ExpandedRow.ExpandedRowEntry? _expandedEntry = ExpandedRowState.Value.GetEntry(_ephemeralViewKey);");
            _ = sb.AppendLine("        object? _expandedItemKey = _expandedEntry?.ItemKey;");
            _ = sb.AppendLine("        " + model.TypeName + "? _expandedItem = null;");
            _ = sb.AppendLine("        if (_expandedItemKey is not null)");
            _ = sb.AppendLine("        {");
            _ = sb.AppendLine("            foreach (var __candidate in state.Items)");
            _ = sb.AppendLine("            {");
            _ = sb.AppendLine("                if (_itemKeyAccessor(__candidate).Equals(_expandedItemKey))");
            _ = sb.AppendLine("                {");
            _ = sb.AppendLine("                    _expandedItem = __candidate;");
            _ = sb.AppendLine("                    break;");
            _ = sb.AppendLine("                }");
            _ = sb.AppendLine("            }");
            _ = sb.AppendLine("        }");
            _ = sb.AppendLine("        bool _expandedItemHiddenByFilter = _expandedItemKey is not null && _expandedItem is null;");
            _ = sb.AppendLine();

            // Banner above the grid (AC8 breadcrumb).
            _ = sb.AppendLine("        builder.OpenComponent<global::Hexalith.FrontComposer.Shell.Components.DataGrid.FcExpandedRowHiddenBanner>(seq++);");
            _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ViewKey\", _viewKey);");
            _ = sb.AppendLine("        builder.AddAttribute(seq++, \"IsHiddenByFilter\", _expandedItemHiddenByFilter);");
            _ = sb.AppendLine("        builder.CloseComponent();");
            _ = sb.AppendLine();
        }

        _ = sb.AppendLine("        builder.OpenComponent<FluentDataGrid<" + model.TypeName + ">>(seq++);");
        // Story 4-4 T2.1 / D1 / D20 — Virtualize default ON. @key on density forces a Virtualize remount
        // when the user toggles density (Fluent v5 reads ItemSize at initialisation).
        _ = sb.AppendLine("        builder.SetKey(_density);");
        _ = sb.AppendLine("        if (state.Items.Count >= ShellOptions.Value.VirtualizationServerSideThreshold)");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            builder.AddAttribute(seq++, \"ItemsProvider\", (global::Microsoft.FluentUI.AspNetCore.Components.GridItemsProvider<" + model.TypeName + ">)LoadPageAsync);");
        _ = sb.AppendLine("        }");
        _ = sb.AppendLine("        else");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            builder.AddAttribute(seq++, \"Items\", " + filteredItemsExpression + ");");
        _ = sb.AppendLine("        }");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"Virtualize\", true);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"DisplayMode\", Microsoft.FluentUI.AspNetCore.Components.DataGridDisplayMode.Table);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ItemSize\", Hexalith.FrontComposer.Shell.Components.Rendering.DataGridDensityMetrics.ResolveRowHeightPx(_density));");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"OverscanCount\", 3);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ItemKey\", (System.Func<" + model.TypeName + ", object>)_itemKeyAccessor);");

        // Story 4-5 D14 / AC1 / AC2 — RowClass highlights the expanded row; OnRowClick toggles.
        if (emitExpandableRows) {
            _ = sb.AppendLine("        builder.AddAttribute(seq++, \"RowClass\", (System.Func<" + model.TypeName + ", string?>)(row => _expandedItemKey is not null && _itemKeyAccessor(row).Equals(_expandedItemKey) ? \"fc-row-expanded\" : null));");
            _ = sb.AppendLine("        builder.AddAttribute(seq++, \"OnRowClick\", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<FluentDataGridRow<" + model.TypeName + ">>(this, row => row?.Item is null ? System.Threading.Tasks.Task.CompletedTask : HandleRowClickAsync(row.Item)));");
        }

        _ = sb.AppendLine();
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder b) =>");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            int colSeq = 200;");

        foreach (ColumnModel col in model.Columns) {
            _ = sb.AppendLine();
            if (model.Columns.Count > 15) {
                _ = sb.AppendLine("            if (!hiddenColumnSet.Contains(\"" + RoleBodyHelpers.EscapeString(col.PropertyName) + "\"))");
                _ = sb.AppendLine("            {");
                ColumnEmitter.EmitColumn(sb, col, model.TypeName, col.PropertyName == defaultSortPropertyName);
                _ = sb.AppendLine("            }");
            }
            else {
                ColumnEmitter.EmitColumn(sb, col, model.TypeName, col.PropertyName == defaultSortPropertyName);
            }
        }

        if (emitRowContextActionColumn) {
            _ = sb.AppendLine();
            EmitRowContextActionColumn(sb, model);
        }

        // Story 4-5 T2.1 / D11 / D14 / D19 / D21 / AC1 / AC7 — trailing row-action TemplateColumn
        // hosting the chevron toggle button. CSS class fc-row-action-column drives the phone-
        // viewport hide rule; fc-expand-button(--open) drives the chevron rotation.
        if (emitExpandableRows) {
            _ = sb.AppendLine();
            EmitExpandTriggerColumn(sb, model);
        }

        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine();
        _ = sb.AppendLine("        builder.CloseComponent();");

        // Story 4-5 T2.1 / D6 / D7 / D19 / AC1 / AC3 — detail panel renders OUTSIDE the
        // virtualized grid (below it, sibling to the FluentDataGrid) so variable-height
        // expansion does NOT interfere with Virtualize's constant-ItemSize math (D6).
        if (emitExpandableRows) {
            _ = sb.AppendLine();
            EmitExpandInRowDetailPanel(sb, model);
        }
    }

    /// <summary>
    /// Story 4-5 T2.1 / D11 / D14 / D19 / D21 / AC1 / AC7 — trailing TemplateColumn carrying
    /// <c>Class="fc-row-action-column"</c> (the phone-viewport hide target per UX-DR62) with
    /// a single <c>FluentButton</c> chevron whose CSS modifier <c>fc-expand-button--open</c>
    /// is bound to the per-row expansion state (three-way guard per D19 extended).
    /// </summary>
    internal static void EmitExpandTriggerColumn(StringBuilder sb, RazorModel model) {
        _ = sb.AppendLine("            // Story 4-5 — trailing row-action column hosting the expand chevron.");
        _ = sb.AppendLine("            b.OpenComponent<TemplateColumn<" + model.TypeName + ">>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Class\", \"fc-row-action-column\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Width\", \"40px\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"ChildContent\", (RenderFragment<" + model.TypeName + ">)(item => (RenderTreeBuilder rb) =>");
        _ = sb.AppendLine("            {");
        _ = sb.AppendLine("                bool _isThisRowExpanded = _expandedItemKey is not null && _itemKeyAccessor(item).Equals(_expandedItemKey) && _expandedItem is not null;");
        _ = sb.AppendLine("                string _buttonClass = _isThisRowExpanded ? \"fc-expand-button fc-expand-button--open\" : \"fc-expand-button\";");
        _ = sb.AppendLine("                string _ariaLabelKey = _isThisRowExpanded ? \"CollapseRowButtonAriaLabelTemplate\" : \"ExpandRowButtonAriaLabelTemplate\";");
        _ = sb.AppendLine("                string _ariaLabel = " + ColumnEmitter.ShellLocalizerFieldName + "[_ariaLabelKey, _itemKeyAccessor(item).ToString() ?? string.Empty].Value;");
        _ = sb.AppendLine("                rb.OpenComponent<global::Microsoft.FluentUI.AspNetCore.Components.FluentButton>(0);");
        _ = sb.AppendLine("                rb.AddAttribute(1, \"Appearance\", global::Microsoft.FluentUI.AspNetCore.Components.ButtonAppearance.Subtle);");
        _ = sb.AppendLine("                rb.AddAttribute(2, \"IconStart\", global::Hexalith.FrontComposer.Shell.Components.Icons.FcFluentIcons.ChevronRight20());");
        _ = sb.AppendLine("                rb.AddAttribute(3, \"Class\", _buttonClass);");
        _ = sb.AppendLine("                rb.AddAttribute(4, \"aria-label\", _ariaLabel);");
        _ = sb.AppendLine("                rb.AddAttribute(5, \"aria-expanded\", _isThisRowExpanded ? \"true\" : \"false\");");
        _ = sb.AppendLine("                rb.AddAttribute(6, \"aria-controls\", _expandPanelId);");
        _ = sb.AppendLine("                rb.AddAttribute(7, \"OnClick\", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, _ => HandleRowClickAsync(item)));");
        _ = sb.AppendLine("                rb.CloseComponent();");
        _ = sb.AppendLine("            }));");
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    private static void EmitStatusOverviewExpandTriggerColumn(StringBuilder sb, string recordTypeName) {
        _ = sb.AppendLine("            // Story 4-5 — trailing row-action column hosting the StatusOverview expand chevron.");
        _ = sb.AppendLine("            b.OpenComponent<TemplateColumn<" + recordTypeName + ">>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Class\", \"fc-row-action-column\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Width\", \"40px\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"ChildContent\", (RenderFragment<" + recordTypeName + ">)(item => (RenderTreeBuilder rb) =>");
        _ = sb.AppendLine("            {");
        _ = sb.AppendLine("                bool _isThisRowExpanded = _expandedItemKey is not null && string.Equals(item.StatusLabel, _expandedItemKey.ToString(), StringComparison.Ordinal) && _expandedStatusOverviewItem is not null;");
        _ = sb.AppendLine("                string _buttonClass = _isThisRowExpanded ? \"fc-expand-button fc-expand-button--open\" : \"fc-expand-button\";");
        _ = sb.AppendLine("                string _ariaLabelKey = _isThisRowExpanded ? \"CollapseRowButtonAriaLabelTemplate\" : \"ExpandRowButtonAriaLabelTemplate\";");
        _ = sb.AppendLine("                string _ariaLabel = " + ColumnEmitter.ShellLocalizerFieldName + "[_ariaLabelKey, item.StatusLabel].Value;");
        _ = sb.AppendLine("                rb.OpenComponent<global::Microsoft.FluentUI.AspNetCore.Components.FluentButton>(0);");
        _ = sb.AppendLine("                rb.AddAttribute(1, \"Appearance\", global::Microsoft.FluentUI.AspNetCore.Components.ButtonAppearance.Subtle);");
        _ = sb.AppendLine("                rb.AddAttribute(2, \"IconStart\", global::Hexalith.FrontComposer.Shell.Components.Icons.FcFluentIcons.ChevronRight20());");
        _ = sb.AppendLine("                rb.AddAttribute(3, \"Class\", _buttonClass);");
        _ = sb.AppendLine("                rb.AddAttribute(4, \"aria-label\", _ariaLabel);");
        _ = sb.AppendLine("                rb.AddAttribute(5, \"aria-expanded\", _isThisRowExpanded ? \"true\" : \"false\");");
        _ = sb.AppendLine("                rb.AddAttribute(6, \"aria-controls\", _expandPanelId);");
        _ = sb.AppendLine("                rb.AddAttribute(7, \"OnClick\", global::Microsoft.AspNetCore.Components.EventCallback.Factory.Create<global::Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, _ => HandleStatusOverviewRowClickAsync(item)));");
        _ = sb.AppendLine("                rb.CloseComponent();");
        _ = sb.AppendLine("            }));");
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    /// <summary>
    /// Story 4-5 T2.1 / D6 / D10 / D19 / AC1 / AC3 / AC4 / AC5 — emits the FcExpandInRowDetail
    /// wrapper after the FluentDataGrid closing tag, with the EmitDetailRecordBody output as
    /// its ChildContent. The wrapper's container is always rendered (D19 extended) so the
    /// trigger button's <c>aria-controls</c> always resolves.
    /// </summary>
    internal static void EmitExpandInRowDetailPanel(StringBuilder sb, RazorModel model) {
        _ = sb.AppendLine("        // Story 4-5 — detail panel host; ChildContent is the factored EmitDetailRecordBody output.");
        _ = sb.AppendLine("        builder.OpenComponent<global::Hexalith.FrontComposer.Shell.Components.DataGrid.FcExpandInRowDetail>(seq++);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ViewKey\", _ephemeralViewKey);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"PanelId\", _expandPanelId);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"HasExpanded\", _expandedItem is not null);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"DetailPanelAriaLabel\", " + ColumnEmitter.ShellLocalizerFieldName + "[\"ExpandInRowDetailPanelAriaLabelTemplate\", _expandedItemKey?.ToString() ?? string.Empty].Value);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"SuppressedAnnouncement\", _expandedItemHiddenByFilter ? " + ColumnEmitter.ShellLocalizerFieldName + "[\"ExpandInRowDetailSuppressedByFilterAnnouncement\"].Value : null);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder builder) =>");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            if (_expandedItem is null) { return; }");
        _ = sb.AppendLine("            int seq = 800;");

        EmitDetailRecordBody(
            sb,
            model,
            builderName: "builder",
            itemExpression: "_expandedItem",
            seqStart: 800,
            groupByFieldGroup: true);

        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine("        builder.CloseComponent();");
    }

    private static void EmitStatusOverviewDetailPanel(StringBuilder sb, RazorModel model, string recordTypeName) {
        _ = sb.AppendLine("        // Story 4-5 — StatusOverview detail panel host; detail body uses the first item in the expanded status group.");
        _ = sb.AppendLine("        builder.OpenComponent<global::Hexalith.FrontComposer.Shell.Components.DataGrid.FcExpandInRowDetail>(seq++);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ViewKey\", _ephemeralViewKey);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"PanelId\", _expandPanelId);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"HasExpanded\", _expandedStatusOverviewItem?.DetailItem is not null);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"DetailPanelAriaLabel\", " + ColumnEmitter.ShellLocalizerFieldName + "[\"ExpandInRowDetailPanelAriaLabelTemplate\", _expandedItemKey?.ToString() ?? string.Empty].Value);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"SuppressedAnnouncement\", _expandedItemHiddenByFilter ? " + ColumnEmitter.ShellLocalizerFieldName + "[\"ExpandInRowDetailSuppressedByFilterAnnouncement\"].Value : null);");
        _ = sb.AppendLine("        builder.AddAttribute(seq++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder builder) =>");
        _ = sb.AppendLine("        {");
        _ = sb.AppendLine("            if (_expandedStatusOverviewItem?.DetailItem is null) { return; }");
        _ = sb.AppendLine("            int seq = 800;");

        EmitDetailRecordBody(
            sb,
            model,
            builderName: "builder",
            itemExpression: "_expandedStatusOverviewItem.DetailItem",
            seqStart: 800,
            groupByFieldGroup: true);

        _ = sb.AppendLine("        }));");
        _ = sb.AppendLine("        builder.CloseComponent();");
    }

    /// <summary>
    /// Story 4-1 D14 / G12 — emits a trailing <c>TemplateColumn</c> whose
    /// <c>ChildContent</c> constructs the per-row <c>ProjectionContext</c> as a
    /// loop-local (NOT a class field — round-4 code-shape invariant) and wraps an
    /// empty render fragment in <c>&lt;CascadingValue Value="_rowContext" IsFixed="true"&gt;</c>.
    /// Story 2-2's inline-command renderer integration fills the fragment without
    /// touching the generator. The column carries CSS class
    /// <c>fc-row-context-actions</c> so adopter style sheets can size or hide the
    /// trailing action surface as appropriate.
    /// </summary>
    internal static void EmitRowContextActionColumn(StringBuilder sb, RazorModel model) {
        string? aggregateIdProperty = RoleBodyHelpers.ResolveAggregateIdProperty(model);
        string aggregateIdExpression = ResolveAggregateIdExpression(model, aggregateIdProperty);
        string projectionFqn = string.IsNullOrEmpty(model.Namespace)
            ? model.TypeName
            : model.Namespace + "." + model.TypeName;
        string boundedContext = string.IsNullOrEmpty(model.BoundedContext)
            ? model.Namespace
            : model.BoundedContext!;

        _ = sb.AppendLine("            // Story 4-1 D14 / G12 — per-row ProjectionContext cascade for Story 2-2 inline-command renderers");
        _ = sb.AppendLine("            b.OpenComponent<TemplateColumn<" + model.TypeName + ">>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Class\", \"fc-row-context-actions\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"ChildContent\", (RenderFragment<" + model.TypeName + ">)(item => (RenderTreeBuilder rb) =>");
        _ = sb.AppendLine("            {");
        _ = sb.AppendLine("                var _rowContext = new global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext(");
        _ = sb.AppendLine("                    projectionTypeFqn: \"" + RoleBodyHelpers.EscapeString(projectionFqn) + "\",");
        _ = sb.AppendLine("                    boundedContext: \"" + RoleBodyHelpers.EscapeString(boundedContext) + "\",");
        _ = sb.AppendLine("                    aggregateId: " + aggregateIdExpression + ",");
        _ = sb.AppendLine("                    fields: global::System.Collections.Immutable.ImmutableDictionary.CreateRange<string, object?>(new[]");
        _ = sb.AppendLine("                    {");

        for (int i = 0; i < model.Columns.Count; i++) {
            ColumnModel col = model.Columns[i];
            string trailingComma = i == model.Columns.Count - 1 ? string.Empty : ",";
            _ = sb.AppendLine(
                "                        new global::System.Collections.Generic.KeyValuePair<string, object?>(\""
                + RoleBodyHelpers.EscapeString(col.PropertyName)
                + "\", (object?)item." + col.PropertyName + ")"
                + trailingComma);
        }

        _ = sb.AppendLine("                    }));");
        _ = sb.AppendLine("                rb.OpenComponent<global::Microsoft.AspNetCore.Components.CascadingValue<global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionContext>>(0);");
        _ = sb.AppendLine("                rb.AddAttribute(1, \"Value\", _rowContext);");
        _ = sb.AppendLine("                rb.AddAttribute(2, \"IsFixed\", true);");
        _ = sb.AppendLine("                rb.AddAttribute(3, \"ChildContent\", (RenderFragment)((RenderTreeBuilder ib) =>");
        _ = sb.AppendLine("                {");
        _ = sb.AppendLine("                    // Story 2-2 inline-command renderer integration fills this fragment.");
        _ = sb.AppendLine("                    // 4-1 ships the cascade contract; the visible action surface lands when");
        _ = sb.AppendLine("                    // the destructive-command pipeline embeds buttons here per AC1b.");
        _ = sb.AppendLine("                }));");
        _ = sb.AppendLine("                rb.CloseComponent();");
        _ = sb.AppendLine("            }));");
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    /// <summary>
    /// Story 4-2 D9 / AC6 / RF2 / RF4 — StatusOverview badge child content. The aggregate row
    /// carries <c>Enum? Status</c> + <c>string StatusLabel</c>; after the null-guard, the
    /// shared <see cref="ColumnEmitter.EmitBadgeSwitch"/> dispatches on the enum member name
    /// so the rendered output matches the DataGrid column path byte-for-byte (including the
    /// localised out-of-range fallback via the view-scoped <c>IStringLocalizer</c>). The
    /// <paramref name="headerLiteral"/> carries the real column header metadata so
    /// <c>aria-label</c> reflects per-projection naming (RF4).
    /// </summary>
    private static void EmitStatusOverviewBadgeChildContent(
        StringBuilder sb,
        ColumnModel statusColumn,
        string recordTypeName,
        string headerLiteral) {
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"ChildContent\", (RenderFragment<" + recordTypeName + ">)(item => (RenderTreeBuilder rb) =>");
        _ = sb.AppendLine("            {");
        _ = sb.AppendLine("                if (item.Status is null)");
        _ = sb.AppendLine("                {");
        _ = sb.AppendLine("                    rb.AddContent(0, item.StatusLabel);");
        _ = sb.AppendLine("                    return;");
        _ = sb.AppendLine("                }");
        _ = sb.AppendLine();
        _ = sb.AppendLine("                var _memberName = item.Status.ToString();");
        _ = sb.AppendLine("                var _label = item.StatusLabel;");
        ColumnEmitter.EmitBadgeSwitch(sb, statusColumn, headerLiteral, builderName: "rb", indent: "                ");
        _ = sb.AppendLine("            }));");
    }

    private static void EmitDetailField(
        StringBuilder sb,
        ColumnModel col,
        int depth,
        string builderName = "b",
        string seqName = "fieldSeq") {
        string indent = new(' ', depth * 4);
        _ = sb.AppendLine(indent + builderName + ".OpenElement(" + seqName + "++, \"div\");");
        _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"class\", \"fc-detail-field\");");
        _ = sb.AppendLine(indent + builderName + ".OpenComponent<FluentLabel>(" + seqName + "++);");
        _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder labelBuilder) =>");
        _ = sb.AppendLine(indent + "{");
        _ = sb.AppendLine(indent + "    labelBuilder.AddContent(0, \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        _ = sb.AppendLine(indent + "}));");
        _ = sb.AppendLine(indent + builderName + ".CloseComponent();");

        // P-1 (Pass-3 review fix): Unsupported columns flowing into DetailRecord / Timeline /
        // StatusOverview detail surfaces must render FcFieldPlaceholder, NOT a bare em-dash.
        // Story 4-6 reversed the silent-drop in RazorModelTransform but the AC6/FR9 contract
        // ("never silently omit") was only honored on the DataGrid path; non-grid roles fell
        // through to the em-dash default arm of FormatValueExpression. This branch closes that
        // regression.
        if (col.TypeCategory == TypeCategory.Unsupported) {
            string typeNameLiteral = "\"" + RoleBodyHelpers.EscapeString(col.UnsupportedTypeFullyQualifiedName ?? col.PropertyName) + "\"";
            string fieldNameLiteral = "\"" + RoleBodyHelpers.EscapeString(col.PropertyName) + "\"";
            _ = sb.AppendLine(indent + builderName + ".OpenComponent<global::Hexalith.FrontComposer.Shell.Components.Rendering.FcFieldPlaceholder>(" + seqName + "++);");
            _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"FieldName\", " + fieldNameLiteral + ");");
            _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"TypeName\", " + typeNameLiteral + ");");
            _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"IsDevMode\", RenderContext?.IsDevMode == true);");
            _ = sb.AppendLine(indent + builderName + ".CloseComponent();");
            EmitDetailDescription(sb, col, indent, builderName, seqName);
            _ = sb.AppendLine(indent + builderName + ".CloseElement();");
            return;
        }

        // Story 4-2 RF1 — annotated enum fields dispatch through FcStatusBadge so DetailRecord
        // reaches the same semantic-color surface as the DataGrid column path.
        if (col.TypeCategory == TypeCategory.Enum && col.BadgeMappings.Count > 0) {
            ColumnEmitter.EmitInlineEnumRenderFragment(
                sb,
                col,
                instanceName: "entity",
                builderName: builderName,
                seqVariable: seqName,
                indent: indent);
            EmitDetailDescription(sb, col, indent, builderName, seqName);
            _ = sb.AppendLine(indent + builderName + ".CloseElement();");
            return;
        }

        _ = sb.AppendLine(indent + builderName + ".OpenComponent<FluentText>(" + seqName + "++);");
        _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder textBuilder) =>");
        _ = sb.AppendLine(indent + "{");
        _ = sb.AppendLine(indent + "    textBuilder.AddContent(0, " + FormatValueExpression(col, "entity") + ");");
        _ = sb.AppendLine(indent + "}));");
        _ = sb.AppendLine(indent + builderName + ".CloseComponent();");
        EmitDetailDescription(sb, col, indent, builderName, seqName);
        _ = sb.AppendLine(indent + builderName + ".CloseElement();");
    }

    private static void EmitDetailDescription(
        StringBuilder sb,
        ColumnModel col,
        string indent,
        string builderName,
        string seqName) {
        if (string.IsNullOrWhiteSpace(col.Description)) {
            return;
        }

        _ = sb.AppendLine(indent + builderName + ".OpenComponent<FluentLabel>(" + seqName + "++);");
        _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"Typography\", Typography.Caption);");
        _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"Class\", \"fc-field-description\");");
        _ = sb.AppendLine(indent + builderName + ".AddAttribute(" + seqName + "++, \"ChildContent\", (RenderFragment)((RenderTreeBuilder descBuilder) =>");
        _ = sb.AppendLine(indent + "{");
        _ = sb.AppendLine(indent + "    descBuilder.AddContent(0, \"" + RoleBodyHelpers.EscapeString(col.Description!) + "\");");
        _ = sb.AppendLine(indent + "}));");
        _ = sb.AppendLine(indent + builderName + ".CloseComponent();");
    }

    private static string ResolveAggregateIdExpression(RazorModel model, string? aggregateIdProperty) {
        if (aggregateIdProperty is null) {
            return "(string?)null";
        }

        ColumnModel? aggregateIdColumn = model.Columns.FirstOrDefault(c => c.PropertyName == aggregateIdProperty);
        if (aggregateIdColumn is null) {
            return "item." + aggregateIdProperty + ".ToString()";
        }

        bool isGuidLikeText = aggregateIdColumn.TypeCategory == TypeCategory.Text
            && aggregateIdColumn.FormatHint?.StartsWith("Truncate:") == true;

        if (aggregateIdColumn.TypeCategory == TypeCategory.Text && !isGuidLikeText) {
            return "item." + aggregateIdProperty;
        }

        if (aggregateIdColumn.IsNullable) {
            return "item." + aggregateIdProperty + ".HasValue ? item." + aggregateIdProperty + ".Value.ToString() : (string?)null";
        }

        return "item." + aggregateIdProperty + ".ToString()";
    }

    private static string FormatValueExpression(ColumnModel col, string instanceName) {
        string formatHint = col.FormatHint ?? string.Empty;
        bool isGuid = formatHint.StartsWith("Truncate:", StringComparison.Ordinal);
        string propertyAccess = instanceName + "." + col.PropertyName;
        return col.TypeCategory switch {
            TypeCategory.Text when isGuid =>
                col.IsNullable
                    ? propertyAccess + ".HasValue ? " + propertyAccess + ".Value.ToString(\"N\") : \"\\u2014\""
                    : propertyAccess + ".ToString(\"N\")",
            TypeCategory.Text =>
                col.IsNullable
                    ? propertyAccess + " ?? \"\\u2014\""
                    : propertyAccess,
            TypeCategory.Numeric =>
                col.IsNullable
                    ? propertyAccess + ".HasValue ? " + propertyAccess + ".Value.ToString(\"" + (col.FormatHint ?? "N0") + "\", CultureInfo.CurrentCulture) : \"\\u2014\""
                    : propertyAccess + ".ToString(\"" + (col.FormatHint ?? "N0") + "\", CultureInfo.CurrentCulture)",
            TypeCategory.Boolean =>
                col.IsNullable
                    ? propertyAccess + ".HasValue ? (" + propertyAccess + ".Value ? \"Yes\" : \"No\") : \"\\u2014\""
                    : "(" + propertyAccess + " ? \"Yes\" : \"No\")",
            TypeCategory.DateTime =>
                col.IsNullable
                    ? propertyAccess + ".HasValue ? " + propertyAccess + ".Value.ToString(\"" + (col.FormatHint ?? "d") + "\", CultureInfo.CurrentCulture) : \"\\u2014\""
                    : propertyAccess + ".ToString(\"" + (col.FormatHint ?? "d") + "\", CultureInfo.CurrentCulture)",
            TypeCategory.Enum =>
                col.IsNullable
                    ? propertyAccess + ".HasValue ? HumanizeEnumLabel(" + propertyAccess + ".Value.ToString()) : \"\\u2014\""
                    : "HumanizeEnumLabel(" + propertyAccess + ".ToString())",
            TypeCategory.Collection =>
                propertyAccess + " == null ? \"\\u2014\" : (System.Linq.Enumerable.Count(" + propertyAccess + ").ToString() + \" items\")",
            _ => "\"\\u2014\"",
        };
    }
}
