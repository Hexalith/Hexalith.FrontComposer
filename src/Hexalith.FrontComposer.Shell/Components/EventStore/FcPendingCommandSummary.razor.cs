using System.Globalization;

using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.EventStore;

/// <summary>Bounded circuit-scoped summary of pending commands resolved after degraded connectivity.</summary>
public partial class FcPendingCommandSummary : ComponentBase, IDisposable {
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <summary>Optional auto-source seam — when <see cref="Entries"/> is empty the component pulls a snapshot from the registered pending-command state service.</summary>
    [Inject]
    private IPendingCommandStateService? PendingCommandState { get; set; }

    [Parameter]
    public IReadOnlyList<PendingCommandEntry> Entries { get; set; } = [];

    [Parameter]
    public int MaxDetails { get; set; } = 5;

    private bool _disposed;

    private IReadOnlyList<PendingCommandEntry> EffectiveEntries =>
        Entries.Count > 0 || PendingCommandState is null
            ? Entries
            : PendingCommandState.Snapshot();

    /// <summary>P19 — guard against zero/negative <see cref="MaxDetails"/> from adopter callers.</summary>
    protected int EffectiveMaxDetails => MaxDetails <= 0 ? 5 : MaxDetails;

    /// <summary>P2-P1 / Story 4.5 — active pending first, then most-recent terminal updates.</summary>
    protected IReadOnlyList<PendingCommandEntry> SummaryEntries =>
        [.. EffectiveEntries
            .OrderBy(static entry => SummaryPriority(entry.Status))
            .ThenByDescending(static entry => entry.TerminalAt ?? entry.SubmittedAt)];

    protected IReadOnlyList<PendingCommandEntry> VisibleEntries =>
        [.. SummaryEntries.Take(EffectiveMaxDetails)];

    protected int OverflowCount => Math.Max(0, SummaryEntries.Count - VisibleEntries.Count);

    protected string SummaryText {
        get {
            int pending = SummaryEntries.Count(static entry => entry.Status == PendingCommandStatus.Pending);
            int confirmed = SummaryEntries.Count(static entry =>
                entry.Status is PendingCommandStatus.Confirmed or PendingCommandStatus.IdempotentConfirmed);
            int rejected = SummaryEntries.Count(static entry => entry.Status == PendingCommandStatus.Rejected);
            int unresolved = SummaryEntries.Count(static entry => entry.Status == PendingCommandStatus.NeedsReview);
            return string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["PendingCommandSummaryCountsTemplate"].Value,
                pending,
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
            PendingCommandStatus.Pending => Localizer["PendingCommandSummaryPendingTemplate"].Value,
            PendingCommandStatus.Confirmed => Localizer["PendingCommandSummaryConfirmedTemplate"].Value,
            PendingCommandStatus.IdempotentConfirmed => Localizer["PendingCommandSummaryAlreadyAppliedTemplate"].Value,
            PendingCommandStatus.NeedsReview => Localizer["PendingCommandSummaryNeedsReviewTemplate"].Value,
            _ => Localizer["PendingCommandSummaryConfirmedTemplate"].Value,
        };

        return string.Format(CultureInfo.CurrentUICulture, template, DisplayName(entry));
    }

    private static int SummaryPriority(PendingCommandStatus status) =>
        status switch {
            PendingCommandStatus.Pending => 0,
            PendingCommandStatus.Rejected => 1,
            PendingCommandStatus.NeedsReview => 2,
            PendingCommandStatus.Confirmed => 3,
            PendingCommandStatus.IdempotentConfirmed => 4,
            _ => 5,
        };

    /// <summary>
    /// DN6 — three-clause rejection format <c>[What failed]: [Why]. [What happened to the data].</c>
    /// The third clause is rendered only when a server-supplied data-impact statement is available
    /// or the caller passes the localizable default explicitly; missing values omit the third
    /// clause to keep older two-clause copy semantics intact.
    /// </summary>
    protected string FormatRejected(PendingCommandEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        string title = NormalizeClause(string.IsNullOrWhiteSpace(entry.RejectionTitle)
            ? string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["PendingCommandSummaryRejectedTitleTemplate"].Value,
                DisplayName(entry))
            : entry.RejectionTitle!);
        string detail = NormalizeClause(string.IsNullOrWhiteSpace(entry.RejectionDetail)
            ? Localizer["PendingCommandSummaryDataImpactDefault"].Value
            : entry.RejectionDetail!);
        string? dataImpact = string.IsNullOrWhiteSpace(entry.RejectionDataImpact)
            ? null
            : NormalizeClause(entry.RejectionDataImpact);
        return dataImpact is null
            ? string.Concat(title, ": ", detail, ".")
            : string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["PendingCommandSummaryRejectedBodyTemplate"].Value,
                title,
                detail,
                dataImpact);
    }

    protected override void OnInitialized() {
        if (PendingCommandState is not null) {
            PendingCommandState.Changed += OnPendingCommandStateChanged;
        }
    }

    public void Dispose() {
        _disposed = true;
        if (PendingCommandState is not null) {
            PendingCommandState.Changed -= OnPendingCommandStateChanged;
        }
    }

    private void OnPendingCommandStateChanged(object? sender, EventArgs e) {
        if (_disposed) {
            return;
        }

        _ = InvokeAsync(() => {
            if (!_disposed) {
                StateHasChanged();
            }
        });
    }

    /// <summary>P2-P13 — strip trailing periods and Unicode whitespace (incl. NBSP) before re-templating.</summary>
    private static string NormalizeClause(string value) =>
        value.AsSpan().TrimEnd(s_trailingPunctuation).ToString();

    private static readonly char[] s_trailingPunctuation = ['.', ' ', '\t', ' ', ' ', ' '];

    /// <summary>
    /// P2-P14 — strip namespace and any generic-arity / nested-type / assembly-qualified suffix.
    /// Generic <c>Type.FullName</c> contains <c>`</c>, <c>+</c>, and <c>[</c>, all of which mark the
    /// end of the simple name; <see cref="string.LastIndexOf(char)"/> on <c>'.'</c> mis-strips into
    /// the assembly-qualified inner name.
    /// </summary>
    private static string DisplayName(PendingCommandEntry entry) {
        string fqn = entry.CommandTypeName;
        if (string.IsNullOrEmpty(fqn)) {
            return fqn;
        }

        int boundary = fqn.IndexOfAny(['`', '[', '+', ',']);
        ReadOnlySpan<char> head = boundary >= 0 ? fqn.AsSpan(0, boundary) : fqn.AsSpan();
        int lastDot = head.LastIndexOf('.');
        return lastDot >= 0 ? head[(lastDot + 1)..].ToString() : head.ToString();
    }
}
