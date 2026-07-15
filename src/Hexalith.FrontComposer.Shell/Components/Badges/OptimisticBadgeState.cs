namespace Hexalith.FrontComposer.Shell.Components.Badges;

/// <summary>Visible optimistic badge state rendered as whole text, not color alone.</summary>
public enum OptimisticBadgeState {
    Confirming,
    Confirmed,
    Rejected,
    IdempotentConfirmed,
    NeedsReview,
}
