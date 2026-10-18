namespace Hexalith.FrontComposer.Testing;

/// <summary>Caller-measured state facts. Null means unmeasured and cannot certify a required SS condition.</summary>
public sealed record UxSurfaceFacts
{
    /// <summary>Whether the surface is visible.</summary>
    public bool? Visible { get; init; }
    /// <summary>Whether denied entries are deliberately hidden by visibility policy.</summary>
    public bool? PolicyHidden { get; init; }
    /// <summary>Whether the surface claims successful completion.</summary>
    public bool? SuccessClaim { get; init; }
    /// <summary>Whether failure, denial, or unconfirmed-outcome text is visible.</summary>
    public bool? FailureVisible { get; init; }
    /// <summary>Whether a loading indicator is visible.</summary>
    public bool? LoadingVisible { get; init; }
    /// <summary>Whether the result is explicitly empty.</summary>
    public bool? Empty { get; init; }
    /// <summary>Whether settled data is visible.</summary>
    public bool? DataVisible { get; init; }
    /// <summary>Whether a non-color text/icon state cue is present.</summary>
    public bool? StateCueVisible { get; init; }
    /// <summary>Whether filters remain available.</summary>
    public bool? FiltersVisible { get; init; }
    /// <summary>Whether status polling is active.</summary>
    public bool? PollingActive { get; init; }
    /// <summary>Whether the original admitted command remains visible.</summary>
    public bool? OriginalOperationVisible { get; init; }
    /// <summary>Whether the current route remains usable.</summary>
    public bool? CurrentRoutePreserved { get; init; }
    /// <summary>Whether the tab is programmatically selected.</summary>
    public bool? Selected { get; init; }
    /// <summary>Whether the tab is programmatically disabled.</summary>
    public bool? Disabled { get; init; }
    /// <summary>Whether the labelled panel is visible.</summary>
    public bool? PanelVisible { get; init; }
    /// <summary>Whether the panel's measured label/control relationships belong to this tab.</summary>
    public bool? PanelOwnedByTab { get; init; }
    /// <summary>Whether the unavailable requested tab is selected.</summary>
    public bool? InvalidTargetSelected { get; init; }
    /// <summary>Whether useful form controls remain editable.</summary>
    public bool? Editable { get; init; }
    /// <summary>Whether the warning remains in the owning page flow.</summary>
    public bool? InFlow { get; init; }
    /// <summary>Whether the overlay is modal.</summary>
    public bool? Modal { get; init; }
    /// <summary>Whether usable focus was restored.</summary>
    public bool? FocusRestored { get; init; }
    /// <summary>Whether updated-row text/icon meaning is visible.</summary>
    public bool? UpdatedCueVisible { get; init; }
    /// <summary>Whether filter-hidden detail is hidden.</summary>
    public bool? DetailHidden { get; init; }
}
