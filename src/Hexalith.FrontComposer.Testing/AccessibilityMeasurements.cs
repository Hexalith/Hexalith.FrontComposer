namespace Hexalith.FrontComposer.Testing;

/// <summary>Fixed, text-free AE measurements. Optional values retain their meaning in serialized evidence.</summary>
public sealed class AccessibilityMeasurements
{
    /// <summary>Whether the caller's measured acceptance conditions passed; this is evidence, not Product approval.</summary>
    public bool Passed { get; init; }
    /// <summary>Whether measured content was lost.</summary>
    public bool? ContentLoss { get; init; }
    /// <summary>Whether measured content or controls were clipped.</summary>
    public bool? Clipping { get; init; }
    /// <summary>Whether content or controls overlapped.</summary>
    public bool? Overlap { get; init; }
    /// <summary>Whether measured controls were truncated.</summary>
    public bool? ControlTruncation { get; init; }
    /// <summary>Whether all exercised operations remained available.</summary>
    public bool? OperationsAvailable { get; init; }
    /// <summary>Whether measured reading and focus order remained logical.</summary>
    public bool? ReadingOrderLogical { get; init; }
    /// <summary>Whether the selected tab and focus cue remained visible.</summary>
    public bool? SelectedTabVisible { get; init; }
    /// <summary>Whether text/border/icon/shape/current-state cues survived forced colors.</summary>
    public bool? ForcedColorCuesPreserved { get; init; }
    /// <summary>Whether non-essential transition, pulse, and smooth scrolling stopped.</summary>
    public bool? NonEssentialAnimationsSuppressed { get; init; }
    /// <summary>Whether static text/icon/shape meaning remained available.</summary>
    public bool? StateCuesPreserved { get; init; }
    /// <summary>Whether focus changes remained immediate.</summary>
    public bool? FocusChangesImmediate { get; init; }
    /// <summary>Whether measured cue expiry caused zero announcements.</summary>
    public bool? ExpirySilent { get; init; }
    /// <summary>Whether the exercised keyboard paths passed.</summary>
    public bool? KeyboardPassed { get; init; }
    /// <summary>Whether the measured axe audit passed.</summary>
    public bool? AxePassed { get; init; }
    /// <summary>Whether every action remained available without hover.</summary>
    public bool? NoHoverOnlyAction { get; init; }
    /// <summary>Effective CSS layout viewport width after browser zoom; AE-01/02 require a positive width of at most 320 CSS pixels.</summary>
    public double? ViewportWidth { get; init; }
    /// <summary>Browser zoom percentage; AE-02 requires at least 400%.</summary>
    public double? ZoomPercent { get; init; }
    /// <summary>Page overflow in CSS pixels.</summary>
    public double? HorizontalOverflow { get; init; }
    /// <summary>Applied line-height multiplier.</summary>
    public double? LineHeight { get; init; }
    /// <summary>Applied paragraph-spacing multiplier.</summary>
    public double? ParagraphSpacing { get; init; }
    /// <summary>Applied letter-spacing multiplier.</summary>
    public double? LetterSpacing { get; init; }
    /// <summary>Applied word-spacing multiplier.</summary>
    public double? WordSpacing { get; init; }
    /// <summary>Measured contrast ratio; Passed AE-06 evidence requires at least 3:1. Callers still assess the applicable 4.5:1 text requirement.</summary>
    public double? ContrastRatio { get; init; }
    /// <summary>Whether forced colors were active.</summary>
    public bool? ForcedColors { get; init; }
    /// <summary>Whether reduced motion was active.</summary>
    public bool? ReducedMotion { get; init; }
    /// <summary>Number of accessibility findings.</summary>
    public int? FindingCount { get; init; }
    /// <summary>Number of critical accessibility findings.</summary>
    public int? CriticalFindingCount { get; init; }
    /// <summary>Observed announcement count.</summary>
    public int? AnnouncementCount { get; init; }
    /// <summary>Whether WCAG 2.2 AA tags were included in the caller's audit.</summary>
    public bool? Wcag22Aa { get; init; }
    /// <summary>Measured focus target bounds.</summary>
    public UxRect? FocusTarget { get; init; }
    /// <summary>Measured complete focus-indicator paint bounds.</summary>
    public UxRect? FocusIndicator { get; init; }
    /// <summary>Active viewport and ancestor scrollport bounds for focus evidence.</summary>
    public IReadOnlyList<UxRect> ViewportAndScrollports { get; init; } = [];
    /// <summary>Painted bounds of app-owned sticky, overlay, and message occluders, including overflowing descendants. Zero-size bounds represent no painted content.</summary>
    public IReadOnlyList<UxRect> Occluders { get; init; } = [];
    /// <summary>Target-size inventory, including supported exceptions and keyboard paths.</summary>
    public IReadOnlyList<AccessibilityTargetEvidence> Targets { get; init; } = [];
}
