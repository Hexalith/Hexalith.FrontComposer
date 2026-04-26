using System.Globalization;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

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
/// mapping while adding text-first pending/rejected/idempotent state labels via
/// <see cref="IStringLocalizer{T}"/> (DN5).
/// </summary>
public partial class FcDesaturatedBadge : ComponentBase {
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

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
            string valueLabel = ResolvedValueLabel;
            string stateLabel = StateLabel;
            return string.IsNullOrWhiteSpace(valueLabel)
                ? stateLabel
                : string.Concat(stateLabel, " ", valueLabel);
        }
    }

    protected string CssClass =>
        State == OptimisticBadgeState.Confirming
            ? "fc-desaturated-badge fc-desaturated-badge--confirming"
            : "fc-desaturated-badge";

    /// <summary>P10 — wrapper renders its own aria-label so tests assert the contract independently of FcStatusBadge internals.</summary>
    protected string AriaLabel {
        get {
            string column = string.IsNullOrWhiteSpace(ColumnHeader) ? "Status" : ColumnHeader!;
            return string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["OptimisticBadgeAriaLabelTemplate"].Value,
                column,
                StateLabel,
                ResolvedValueLabel);
        }
    }

    private string ResolvedValueLabel =>
        State == OptimisticBadgeState.Rejected ? PriorLabel : OptimisticLabel;

    private string StateLabel => State switch {
        OptimisticBadgeState.Confirming => Localizer["OptimisticBadgeConfirmingLabel"].Value,
        OptimisticBadgeState.Confirmed => Localizer["OptimisticBadgeConfirmedLabel"].Value,
        OptimisticBadgeState.Rejected => Localizer["OptimisticBadgeRejectedLabel"].Value,
        OptimisticBadgeState.IdempotentConfirmed => Localizer["OptimisticBadgeAlreadyAppliedLabel"].Value,
        OptimisticBadgeState.NeedsReview => Localizer["OptimisticBadgeNeedsReviewLabel"].Value,
        _ => Localizer["OptimisticBadgeConfirmingLabel"].Value,
    };
}
