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

    /// <summary>P2-P12 — Rejected and NeedsReview both fall back to the prior slot so colour and text agree (NeedsReview is a "we don't know" outcome, not a success).</summary>
    protected BadgeSlot ResolvedSlot =>
        State is OptimisticBadgeState.Rejected or OptimisticBadgeState.NeedsReview
            ? PriorSlot
            : OptimisticSlot;

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

    /// <summary>P10 — wrapper renders its own aria-label so tests assert the contract independently of FcStatusBadge internals. P2-P11 — fallback header is localized so French parity holds.</summary>
    protected string AriaLabel {
        get {
            string column = string.IsNullOrWhiteSpace(ColumnHeader)
                ? Localizer["OptimisticBadgeDefaultColumnHeader"].Value
                : ColumnHeader!;
            return string.Format(
                CultureInfo.CurrentUICulture,
                Localizer["OptimisticBadgeAriaLabelTemplate"].Value,
                column,
                StateLabel,
                ResolvedValueLabel);
        }
    }

    private string ResolvedValueLabel =>
        State is OptimisticBadgeState.Rejected or OptimisticBadgeState.NeedsReview
            ? PriorLabel
            : OptimisticLabel;

    private string StateLabel => State switch {
        OptimisticBadgeState.Confirming => Localizer["OptimisticBadgeConfirmingLabel"].Value,
        OptimisticBadgeState.Confirmed => Localizer["OptimisticBadgeConfirmedLabel"].Value,
        OptimisticBadgeState.Rejected => Localizer["OptimisticBadgeRejectedLabel"].Value,
        OptimisticBadgeState.IdempotentConfirmed => Localizer["OptimisticBadgeAlreadyAppliedLabel"].Value,
        OptimisticBadgeState.NeedsReview => Localizer["OptimisticBadgeNeedsReviewLabel"].Value,
        _ => Localizer["OptimisticBadgeConfirmingLabel"].Value,
    };
}
