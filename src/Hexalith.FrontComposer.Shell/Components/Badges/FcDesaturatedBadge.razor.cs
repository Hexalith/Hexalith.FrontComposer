using Hexalith.FrontComposer.Contracts.Attributes;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Badges;

/// <summary>Visible optimistic badge state rendered as whole text, not color alone.</summary>
public enum OptimisticBadgeState {
    Confirming,
    Confirmed,
    Rejected,
    IdempotentConfirmed,
    NeedsReview,
}

/// <summary>
/// Story 5-5 optimistic status badge wrapper. Reuses <see cref="FcStatusBadge"/> for semantic color
/// mapping while adding text-first pending/rejected/idempotent state labels.
/// </summary>
public partial class FcDesaturatedBadge : ComponentBase {
    [Parameter]
    [EditorRequired]
    public BadgeSlot PriorSlot { get; set; }

    [Parameter]
    [EditorRequired]
    public string PriorLabel { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public BadgeSlot OptimisticSlot { get; set; }

    [Parameter]
    [EditorRequired]
    public string OptimisticLabel { get; set; } = string.Empty;

    [Parameter]
    public OptimisticBadgeState State { get; set; } = OptimisticBadgeState.Confirming;

    [Parameter]
    public string? ColumnHeader { get; set; }

    protected BadgeSlot ResolvedSlot =>
        State == OptimisticBadgeState.Rejected ? PriorSlot : OptimisticSlot;

    protected string ResolvedLabel {
        get {
            string valueLabel = State == OptimisticBadgeState.Rejected ? PriorLabel : OptimisticLabel;
            string stateLabel = State switch {
                OptimisticBadgeState.Confirming => "Confirming",
                OptimisticBadgeState.Confirmed => "Confirmed",
                OptimisticBadgeState.Rejected => "Rejected",
                OptimisticBadgeState.IdempotentConfirmed => "Already applied",
                OptimisticBadgeState.NeedsReview => "Needs review",
                _ => "Confirming",
            };

            return string.IsNullOrWhiteSpace(valueLabel)
                ? stateLabel
                : string.Concat(stateLabel, " ", valueLabel);
        }
    }

    protected string CssClass =>
        State == OptimisticBadgeState.Confirming
            ? "fc-desaturated-badge fc-desaturated-badge--confirming"
            : "fc-desaturated-badge";
}
