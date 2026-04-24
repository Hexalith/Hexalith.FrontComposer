using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-3 T1.4 / D13 / D21 / AC3 — composes the filter summary string from the
/// snapshot's <c>Filters</c> dictionary + optional sort. Visible only when any filter
/// or non-default sort is active.
/// </summary>
public partial class FcFilterSummary : ComponentBase {
    private bool _visible;
    private string _prefix = string.Empty;
    private List<string> _clauses = [];

    /// <summary>Stable per-view key (reserved for future use).</summary>
    [Parameter]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>Column-keyed filters including the reserved <c>__status</c> / <c>__search</c> keys.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyDictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();

    /// <summary>Column keys → humanised header text.</summary>
    [Parameter]
    public IReadOnlyDictionary<string, string>? HumanisedColumnHeaders { get; set; }

    /// <summary>Column key currently sorted by, or null.</summary>
    [Parameter]
    public string? SortColumn { get; set; }

    /// <summary>Whether the current sort is descending.</summary>
    [Parameter]
    public bool SortDescending { get; set; }

    /// <summary>Row count matching the current filters.</summary>
    [Parameter]
    [EditorRequired]
    public int FilteredCount { get; set; }

    /// <summary>Total row count pre-filter.</summary>
    [Parameter]
    [EditorRequired]
    public int TotalCount { get; set; }

    /// <summary>Humanised entity plural (e.g. "orders").</summary>
    [Parameter]
    [EditorRequired]
    public string EntityPlural { get; set; } = string.Empty;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _clauses = BuildClauses();
        _visible = _clauses.Count > 0;
        _prefix = Localizer[
            "FilterSummaryShowingTemplate",
            FilteredCount.ToString(CultureInfo.CurrentUICulture),
            TotalCount.ToString(CultureInfo.CurrentUICulture),
            EntityPlural].Value;
    }

    private List<string> BuildClauses() {
        List<string> clauses = [];

        IReadOnlyList<string> statusSlots = GetOrderedStatusSlots();
        if (statusSlots.Count > 0) {
            clauses.Add(Localizer["FilterSummaryStatusClauseTemplate", JoinWithLocalizedOr(statusSlots)].Value);
        }

        foreach (KeyValuePair<string, string> kvp in GetOrderedColumnFilters()) {
            string header = ResolveHeader(kvp.Key);
            clauses.Add(Localizer["FilterSummaryColumnContainsTemplate", header, EscapeForQuotedTemplate(kvp.Value)].Value);
        }

        if (TryGetSearchQuery(out string? search) && search is not null) {
            clauses.Add(Localizer["FilterSummarySearchClauseTemplate", EscapeForQuotedTemplate(search)].Value);
        }

        if (!string.IsNullOrWhiteSpace(SortColumn)) {
            clauses.Add(ComposeSortClause(SortColumn!));
        }

        return clauses;
    }

    // Summary templates embed the filter value inside literal quote marks (e.g. `{0} contains "{1}"`
    // / `{0} contient « {1} »`). A user-typed straight quote would visually break the quoted run; swap
    // to typographic quotes so the clause renders unambiguously in both locales.
    private static string EscapeForQuotedTemplate(string value)
        => string.IsNullOrEmpty(value)
            ? value
            : value.Replace("\"", "”", StringComparison.Ordinal);

    private IReadOnlyList<string> GetOrderedStatusSlots() {
        if (!Filters.TryGetValue(ReservedFilterKeys.StatusKey, out string? statusCsv)
            || string.IsNullOrWhiteSpace(statusCsv)) {
            return [];
        }

        return statusCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(static slot => slot.Trim())
            .Where(static slot => slot.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static slot => slot, StringComparer.Ordinal)
            .ToArray();
    }

    private IEnumerable<KeyValuePair<string, string>> GetOrderedColumnFilters()
        => Filters
            .Where(static kvp => !kvp.Key.StartsWith("__", StringComparison.Ordinal))
            .OrderBy(kvp => ResolveHeader(kvp.Key), StringComparer.Ordinal)
            .ThenBy(kvp => kvp.Key, StringComparer.Ordinal);

    private bool TryGetSearchQuery(out string? search) {
        if (Filters.TryGetValue(ReservedFilterKeys.SearchKey, out string? value)
            && !string.IsNullOrWhiteSpace(value)) {
            search = value;
            return true;
        }

        search = null;
        return false;
    }

    private string ResolveHeader(string columnKey)
        => HumanisedColumnHeaders is not null
            && HumanisedColumnHeaders.TryGetValue(columnKey, out string? header)
                ? header
                : columnKey;

    private string JoinWithLocalizedOr(IReadOnlyList<string> values) {
        if (values.Count == 0) {
            return string.Empty;
        }

        if (values.Count == 1) {
            return values[0];
        }

        string conjunction = GetLocalizedOrConjunction();
        if (values.Count == 2) {
            return values[0] + conjunction + values[1];
        }

        return string.Join(", ", values.Take(values.Count - 1)) + conjunction + values[^1];
    }

    private string ComposeSortClause(string sortColumn) {
        string header = ResolveHeader(sortColumn);
        string direction = Localizer[SortDescending ? "SortDirectionDescending" : "SortDirectionAscending"].Value;
        return Localizer["FilterSummarySortClauseTemplate", header, direction].Value;
    }

    private string GetLocalizedOrConjunction()
        => Localizer["FilterSummaryOrConjunction"].Value;
}
