using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.EventStore;

/// <summary>Bounded circuit-scoped summary of pending commands resolved after degraded connectivity.</summary>
public partial class FcPendingCommandSummary : ComponentBase {
    [Parameter]
    public IReadOnlyList<PendingCommandEntry> Entries { get; set; } = [];

    [Parameter]
    public int MaxDetails { get; set; } = 5;

    protected IReadOnlyList<PendingCommandEntry> TerminalEntries =>
        [.. Entries
            .Where(static entry => entry.Status != PendingCommandStatus.Pending)
            .OrderBy(static entry => entry.TerminalAt ?? entry.SubmittedAt)];

    protected IReadOnlyList<PendingCommandEntry> VisibleEntries =>
        [.. TerminalEntries.Take(Math.Max(0, MaxDetails))];

    protected int OverflowCount => Math.Max(0, TerminalEntries.Count - VisibleEntries.Count);

    protected string SummaryText {
        get {
            int confirmed = TerminalEntries.Count(static entry =>
                entry.Status is PendingCommandStatus.Confirmed or PendingCommandStatus.IdempotentConfirmed);
            int rejected = TerminalEntries.Count(static entry => entry.Status == PendingCommandStatus.Rejected);
            int unresolved = TerminalEntries.Count(static entry => entry.Status == PendingCommandStatus.NeedsReview);
            return $"{confirmed} confirmed, {rejected} rejected, {unresolved} unresolved";
        }
    }

    protected static string FormatEntry(PendingCommandEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        return entry.Status switch {
            PendingCommandStatus.Confirmed => $"{entry.CommandTypeName}: confirmed.",
            PendingCommandStatus.IdempotentConfirmed => $"{entry.CommandTypeName}: already applied.",
            PendingCommandStatus.NeedsReview => $"{entry.CommandTypeName}: unresolved. Review the current data before retrying.",
            _ => $"{entry.CommandTypeName}: pending.",
        };
    }

    protected static string FormatRejected(PendingCommandEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);

        string title = string.IsNullOrWhiteSpace(entry.RejectionTitle)
            ? $"{entry.CommandTypeName} failed"
            : entry.RejectionTitle!;
        string detail = string.IsNullOrWhiteSpace(entry.RejectionDetail)
            ? "No data changed."
            : entry.RejectionDetail!;
        return $"{title}: {detail}";
    }
}
