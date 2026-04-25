using System.Collections.Generic;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

namespace Hexalith.FrontComposer.SourceTools.Emitters;

/// <summary>
/// Story 4-1 — shared static helpers consumed by <see cref="ProjectionRoleBodyEmitter"/>
/// for the property-resolution heuristics D10/D11 (status-enum property), D13 (first
/// DateTime property), and AggregateId conventions (D14). Kept internal so the emit
/// stage owns the canonical rule; Story 4.3 will consolidate D10/D11 once a third
/// consumer (filter UI) exists.
/// </summary>
internal static class RoleBodyHelpers {
    /// <summary>
    /// Story 4-1 D10/D11 — deterministic tiebreaker for the single status-enum
    /// property. Priority: (1) enum-typed column whose <see cref="ColumnModel.BadgeMappings"/>
    /// is non-empty (the <c>[ProjectionBadge]</c>-carrying enum); (2) first enum-typed
    /// column in declaration order. Ties broken by declaration order.
    /// </summary>
    public static string? ResolveStatusEnumProperty(RazorModel model) {
        return ResolveStatusEnumColumn(model)?.PropertyName;
    }

    public static ColumnModel? ResolveStatusEnumColumn(RazorModel model) {
        ColumnModel? badgedEnum = null;
        ColumnModel? firstEnum = null;

        foreach (ColumnModel col in model.Columns) {
            if (col.TypeCategory != TypeCategory.Enum) {
                continue;
            }

            firstEnum ??= col;

            if (badgedEnum is null && col.BadgeMappings.Count > 0) {
                badgedEnum = col;
            }
        }

        return badgedEnum ?? firstEnum;
    }

    /// <summary>
    /// Story 4-1 D13 — deterministic tiebreaker for the Timeline ordering property.
    /// Returns the first <c>DateTime</c>/<c>DateTimeOffset</c> column in declaration
    /// order. Returns null when no DateTime property exists on the projection; the
    /// caller falls back to declaration-order rendering and Transform emits HFC1022.
    /// </summary>
    public static string? ResolveFirstDateTimeProperty(RazorModel model) {
        return ResolveFirstDateTimeColumn(model)?.PropertyName;
    }

    public static ColumnModel? ResolveFirstDateTimeColumn(RazorModel model) {
        foreach (ColumnModel col in model.Columns) {
            if (col.TypeCategory == TypeCategory.DateTime) {
                return col;
            }
        }

        return null;
    }

    /// <summary>
    /// Story 4-1 Timeline row composition — returns the first text column used as a
    /// row label next to the timestamp.
    /// </summary>
    public static string? ResolveFirstTextProperty(RazorModel model) {
        return ResolveFirstTextColumn(model)?.PropertyName;
    }

    public static ColumnModel? ResolveFirstTextColumn(RazorModel model) {
        foreach (ColumnModel col in model.Columns) {
            if (col.TypeCategory == TypeCategory.Text) {
                return col;
            }
        }

        return null;
    }

    /// <summary>
    /// Story 4-1 AC4 — prefer a descriptive domain label for Timeline rows instead of
    /// blindly picking the first text column. The heuristic favors descriptive names
    /// (<c>Name</c>, <c>Title</c>, <c>Description</c>, etc.), then any non-id text
    /// column, and only falls back to id-like columns when no richer text field exists.
    /// This keeps Guid-like identifiers from crowding out the human-readable event label
    /// while preserving the existing <c>Id</c>-only fallback used by generic fixtures.
    /// </summary>
    public static string? ResolveTimelineLabelProperty(RazorModel model) {
        return ResolveTimelineLabelColumn(model)?.PropertyName;
    }

    public static ColumnModel? ResolveTimelineLabelColumn(RazorModel model) {
        ColumnModel? preferred = null;
        ColumnModel? nonIdText = null;
        ColumnModel? idLikeText = null;

        foreach (ColumnModel col in model.Columns) {
            if (col.TypeCategory != TypeCategory.Text) {
                continue;
            }

            if (preferred is null && IsPreferredTimelineLabelProperty(col.PropertyName)) {
                preferred = col;
            }

            if (IsIdLikeProperty(model, col.PropertyName)) {
                idLikeText ??= col;
                continue;
            }

            nonIdText ??= col;
        }

        return preferred ?? nonIdText ?? idLikeText;
    }

    /// <summary>
    /// Story 4-1 D14 — AggregateId convention scan. Ordinal-case-insensitive match
    /// against "Id" / "AggregateId" / "{TypeName}Id" in declaration order. Returns
    /// null when no matching property is found; per-row cascade tolerates a null
    /// aggregate id (valid for read-only timeline projections).
    /// </summary>
    public static string? ResolveAggregateIdProperty(RazorModel model) {
        foreach (ColumnModel col in model.Columns) {
            if (IsIdLikeProperty(model, col.PropertyName)) {
                return col.PropertyName;
            }
        }

        return null;
    }

    public static ColumnModel? ResolveActionQueueSortColumn(RazorModel model) {
        ColumnModel? firstColumn = null;
        ColumnModel? badgedEnum = null;
        ColumnModel? firstDateTime = null;

        foreach (ColumnModel col in model.Columns) {
            firstColumn ??= col;

            if (badgedEnum is null
                && col.TypeCategory == TypeCategory.Enum
                && col.BadgeMappings.Count > 0) {
                badgedEnum = col;
            }

            if (firstDateTime is null && col.TypeCategory == TypeCategory.DateTime) {
                firstDateTime = col;
            }
        }

        return badgedEnum ?? firstDateTime ?? firstColumn;
    }

    public static string? ResolveWhenStateFilterPredicate(RazorModel model) {
        if (model.WhenStates.Count == 0) {
            return null;
        }

        string? statusProperty = ResolveStatusEnumProperty(model);
        if (statusProperty is null) {
            return null;
        }

        List<string> comparisons = [];
        foreach (string state in model.WhenStates) {
            comparisons.Add(
                "x."
                + statusProperty
                + ".ToString() == \""
                + EscapeString(state)
                + "\"");
        }

        return comparisons.Count == 0 ? null : string.Join(" || ", comparisons);
    }

    /// <summary>C-style string escape for StringBuilder literal emission.</summary>
    public static string EscapeString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>
    /// Story 4-4 T2 / D2 — strategies that emit a <c>FluentDataGrid</c> and therefore receive
    /// the virtualization envelope (banners, outer scroll container, OnAfterRenderAsync hooks,
    /// IAsyncDisposable). DetailRecord and Timeline render <c>FluentCard</c>/<c>FluentStack</c>
    /// and are exempt.
    /// </summary>
    public static bool IsGridRenderingStrategy(ProjectionRenderStrategy strategy)
        => strategy == ProjectionRenderStrategy.Default
            || strategy == ProjectionRenderStrategy.ActionQueue
            || strategy == ProjectionRenderStrategy.StatusOverview
            || strategy == ProjectionRenderStrategy.Dashboard;

    /// <summary>
    /// Story 4-4 T2.1 / D13 / D18 — first-match item-key accessor expression. Precedence:
    /// <c>AggregateId</c> &gt; <c>Id</c> &gt; <c>Key</c> &gt; <c>x =&gt; (object)x</c> fallback.
    /// Resolved at emit time so the generated view contains a baked-in lambda — no runtime
    /// reflection (D8 trim/AOT discipline). Returns the lambda body without the lambda
    /// keyword: caller wraps in <c>x =&gt; (object)( ... )</c>.
    /// </summary>
    public static string ResolveItemKeyAccessorExpression(RazorModel model) {
        string? aggregateIdProp = null;
        string? idProp = null;
        string? keyProp = null;

        foreach (ColumnModel col in model.Columns) {
            if (aggregateIdProp is null
                && string.Equals(col.PropertyName, "AggregateId", StringComparison.Ordinal)) {
                aggregateIdProp = col.PropertyName;
            }
            else if (idProp is null
                && string.Equals(col.PropertyName, "Id", StringComparison.Ordinal)) {
                idProp = col.PropertyName;
            }
            else if (keyProp is null
                && string.Equals(col.PropertyName, "Key", StringComparison.Ordinal)) {
                keyProp = col.PropertyName;
            }
        }

        string? selected = aggregateIdProp ?? idProp ?? keyProp;
        return selected is null
            ? "(object)x"
            : "(object)x." + selected + "!";
    }

    /// <summary>
    /// Story 4-4 T2.5 / D4 — stable per-view key consumed by the @onscroll handler, scroll
    /// restore interop, ClearPendingPagesAction sweep, and persistence keys. Format:
    /// <c>{boundedContext}:{Namespace}.{TypeName}</c>. Bounded context defaults to namespace
    /// when not explicitly declared.
    /// </summary>
    public static string ResolveViewKey(RazorModel model) {
        string bc = string.IsNullOrEmpty(model.BoundedContext)
            ? model.Namespace
            : model.BoundedContext!;
        string typeFqn = string.IsNullOrEmpty(model.Namespace)
            ? model.TypeName
            : model.Namespace + "." + model.TypeName;
        return bc + ":" + typeFqn;
    }

    /// <summary>
    /// Story 4-5 T6.5 / D9 / AC5 — partitions a column list (typically the secondary detail
    /// columns at positions 7+) into ordered groups for <c>FluentAccordionItem</c> emission.
    /// Group ordering is first-declared-property precedence (stable); the catch-all bucket
    /// (returned with <c>GroupName == null</c>) collects every column whose
    /// <see cref="ColumnModel.FieldGroup"/> is null and is appended LAST. When no column
    /// in the input declares a <see cref="ColumnModel.FieldGroup"/>, the result is a single
    /// catch-all bucket carrying every input column unchanged — letting the emitter fall
    /// back to the Story 4-1 single-accordion layout (regression gate for 4-1's approval
    /// baselines).
    /// </summary>
    /// <param name="columns">The columns to partition (declaration order preserved within each group).</param>
    /// <returns>An ordered list of (group-name, columns) pairs. Empty input yields empty output.</returns>
    public static IReadOnlyList<FieldGroupBucket> ResolveFieldGroups(IReadOnlyList<ColumnModel> columns) {
        if (columns is null || columns.Count == 0) {
            return Array.Empty<FieldGroupBucket>();
        }

        // Detect whether ANY column declares a [ProjectionFieldGroup] annotation. When zero,
        // short-circuit to the legacy single-bucket form so 4-1 approval baselines stay green.
        bool anyAnnotated = false;
        for (int i = 0; i < columns.Count; i++) {
            if (columns[i].FieldGroup is not null) {
                anyAnnotated = true;
                break;
            }
        }

        if (!anyAnnotated) {
            return new[] { new FieldGroupBucket(null, columns) };
        }

        // Group-order discovery: walk in declaration order, emit a new bucket the first time
        // a non-null FieldGroup is seen, append subsequent same-name columns to that bucket.
        // Ungrouped columns go into a deferred catch-all that's appended at the end.
        Dictionary<string, List<ColumnModel>> grouped = new(StringComparer.Ordinal);
        List<string> orderedNames = new();
        List<ColumnModel> ungrouped = new();
        for (int i = 0; i < columns.Count; i++) {
            ColumnModel col = columns[i];
            if (col.FieldGroup is null) {
                ungrouped.Add(col);
                continue;
            }

            if (!grouped.TryGetValue(col.FieldGroup, out List<ColumnModel>? bucket)) {
                bucket = new List<ColumnModel>();
                grouped.Add(col.FieldGroup, bucket);
                orderedNames.Add(col.FieldGroup);
            }

            bucket.Add(col);
        }

        List<FieldGroupBucket> result = new(orderedNames.Count + (ungrouped.Count > 0 ? 1 : 0));
        for (int i = 0; i < orderedNames.Count; i++) {
            string name = orderedNames[i];
            result.Add(new FieldGroupBucket(name, grouped[name]));
        }

        if (ungrouped.Count > 0) {
            result.Add(new FieldGroupBucket(null, ungrouped));
        }

        return result;
    }

    private static bool IsPreferredTimelineLabelProperty(string propertyName)
        => propertyName.EndsWith("Name", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Title", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Label", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Description", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Summary", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Subject", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Message", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Event", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Activity", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Details", StringComparison.OrdinalIgnoreCase);

    private static bool IsIdLikeProperty(RazorModel model, string propertyName) {
        string typeIdCandidate = StripProjectionSuffix(model.TypeName) + "Id";
        return string.Equals(propertyName, "Id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, "AggregateId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(propertyName, typeIdCandidate, StringComparison.OrdinalIgnoreCase);
    }

    private static string StripProjectionSuffix(string typeName)
        => typeName.EndsWith("Projection", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "Projection".Length)
            : typeName;
}
