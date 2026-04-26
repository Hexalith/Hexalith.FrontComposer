using System.Globalization;

using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.EventStore;

/// <summary>Bounded circuit-scoped summary of pending commands resolved after degraded connectivity.</summary>
public partial class FcPendingCommandSummary : ComponentBase {
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <summary>Optional auto-source seam — when <see cref="Entries"/> is empty the component pulls a snapshot from the registered pending-command state service.</summary>
    [Inject]
    private IPendingCommandStateService? PendingCommandState { get; set; }

    [Parameter]
    public IReadOnlyList<PendingCommandEntry> Entries { get; set; } = [];

    [Parameter]
    public int MaxDetails { get; set; } = 5;

    private IReadOnlyList<PendingCommandEntry> EffectiveEntries =>
        Entries.Count > 0 || PendingCommandState is null
            ? Entries
            : PendingCommandState.Snapshot();

    /// <summary>P19 — guard against zero/negative <see cref="MaxDetails"/> from adopter callers.</summary>
    protected int EffectiveMaxDetails => MaxDetails <= 0 ? 5 : MaxDetails;

    protected IReadOnlyList<PendingCommandEntry> TerminalEntries =>
        [.. EffectiveEntries
            .Where(static entry => entry.Status != PendingCommandStatus.Pending)
            .OrderBy(static entry => entry.TerminalAt ?? entry.SubmittedAt)];

    protected IReadOnlyList<PendingCommandEntry> VisibleEntries =>
        [.. TerminalEntries.Take(EffectiveMaxDetails)];

    protected int OverflowCount => Math.Max(0, TerminalEntries.Count - VisibleEntries.Count);

    protected string SummaryText {
        get {
            int confirmed = TerminalEntries.Count(static entry =>
                entry.Status is PendingCommandStatus.Confirmed or PendingCommandStatus.IdempotentConfirmed);
            int rejected = TerminalEntries.Count(static entry => entry.Status == PendingCommandStatus.Rejected);
            int unresolved = TerminalEntries.Count(static entry => entry.Status == PendingCommandStatus.NeedsReview);
            return string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["PendingCommandSummaryCountsTemplate"].Value,
                confirmed,
                rejected,
                unresolved);
        }
    }

    protected string OverflowText => string.Format(
        CultureInfo.CurrentUICulture,
        Localizer["PendingCommandSummaryOverflowTemplate"].Value,
        OverflowCount);

    protected string FormatEntry(PendingCommandEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        string template = entry.Status switch {
            PendingCommandStatus.Confirmed => Localizer["PendingCommandSummaryConfirmedTemplate"].Value,
            PendingCommandStatus.IdempotentConfirmed => Localizer["PendingCommandSummaryAlreadyAppliedTemplate"].Value,
            PendingCommandStatus.NeedsReview => Localizer["PendingCommandSummaryNeedsReviewTemplate"].Value,
            _ => Localizer["PendingCommandSummaryConfirmedTemplate"].Value,
        };

        return string.Format(CultureInfo.CurrentUICulture, template, DisplayName(entry));
    }

    /// <summary>
    /// DN6 — three-clause rejection format <c>[What failed]: [Why]. [What happened to the data].</c>
    /// The third clause is rendered only when a server-supplied data-impact statement is available
    /// or the caller passes the localizable default explicitly; missing values omit the third
    /// clause to keep older two-clause copy semantics intact.
    /// </summary>
    protected string FormatRejected(PendingCommandEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        string title = string.IsNullOrWhiteSpace(entry.RejectionTitle)
            ? string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["PendingCommandSummaryRejectedTitleTemplate"].Value,
                DisplayName(entry))
            : entry.RejectionTitle!;
        string detail = string.IsNullOrWhiteSpace(entry.RejectionDetail)
            ? Localizer["PendingCommandSummaryDataImpactDefault"].Value
            : entry.RejectionDetail!.TrimEnd('.', ' ');
        string? dataImpact = string.IsNullOrWhiteSpace(entry.RejectionDataImpact) ? null : entry.RejectionDataImpact;
        return dataImpact is null
            ? string.Concat(title, ": ", detail, ".")
            : string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["PendingCommandSummaryRejectedBodyTemplate"].Value,
                title,
                detail,
                dataImpact);
    }

    private static string DisplayName(PendingCommandEntry entry) =>
        entry.CommandTypeName.Contains('.', StringComparison.Ordinal)
            ? entry.CommandTypeName[(entry.CommandTypeName.LastIndexOf('.') + 1)..]
            : entry.CommandTypeName;
}
